using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("L14 Keyboard Light")]
[assembly: AssemblyDescription("Lenovo OEM driver keyboard backlight control for ThinkPad L14 Gen 3 21C2")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace L14KeyboardLight
{
    internal static class Program
    {
        internal const string StopEvent = @"Local\L14KeyboardLight-Stop-v1";
        [STAThread]
        private static int Main(string[] args)
        {
            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch (EntryPointNotFoundException) { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                if (args.Length > 0 && args[0] == "--stop")
                {
                    try { using (EventWaitHandle stop = EventWaitHandle.OpenExisting(StopEvent)) stop.Set(); }
                    catch (WaitHandleCannotBeOpenedException) { }
                    return 0;
                }
                if (args.Length == 2 && args[0] == "--self-test")
                {
                    using (StreamWriter output = new StreamWriter(args[1], false, Encoding.UTF8))
                    {
                        int policy = PolicyTests.Run(output);
                        int fullscreen = FullscreenTests.Run(output);
                        output.WriteLine("OVERALL: " + (policy == 0 && fullscreen == 0 ? "PASS" : "FAIL"));
                        return policy == 0 && fullscreen == 0 ? 0 : 1;
                    }
                }
                if (args.Length == 2 && args[0] == "--status")
                {
                    LenovoBacklightDevice device = new LenovoBacklightDevice();
                    File.WriteAllText(args[1], "Model: " + device.Model + Environment.NewLine + "Family: " + device.Family +
                        Environment.NewLine + "Level: " + device.ReadLevel() + Environment.NewLine + FullscreenDetector.DescribeForeground(), Encoding.UTF8);
                    return 0;
                }
                if (args.Length > 0) return 2;
                bool created;
                using (Mutex mutex = new Mutex(true, @"Local\L14KeyboardLight-v1", out created))
                {
                    if (!created) return 0;
                    try
                    {
                        using (EventWaitHandle stop = new EventWaitHandle(false, EventResetMode.AutoReset, StopEvent))
                        using (TrayApplication app = new TrayApplication(stop)) Application.Run(app);
                    }
                    finally { mutex.ReleaseMutex(); }
                }
                return 0;
            }
            catch (Exception error)
            {
                Settings.Log(error.ToString());
                if (args.Length == 2)
                {
                    try { File.WriteAllText(args[1], "ERROR: " + error.ToString(), Encoding.UTF8); } catch { }
                }
                else MessageBox.Show(error.Message, "L14 Keyboard Light", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    }

    internal sealed class TrayApplication : ApplicationContext
    {
        private readonly LenovoBacklightDevice device;
        private readonly Settings settings;
        private readonly BacklightPolicy policy;
        private readonly EventWaitHandle stopEvent;
        private readonly PowerWindow power;
        private readonly NotifyIcon tray;
        private readonly Icon icon;
        private readonly ContextMenuStrip menu;
        private readonly ToolStripMenuItem[] levels = new ToolStripMenuItem[3];
        private readonly ToolStripMenuItem automatic;
        private readonly ToolStripMenuItem startup;
        private readonly System.Windows.Forms.Timer timer;
        private bool wasActive;
        private bool resumePending;
        private DateTime settleUntil = DateTime.MinValue;
        private DateTime retryAfter = DateTime.MinValue;
        private string lastError;
        private string lastState;
        private bool exiting;

        public TrayApplication(EventWaitHandle stop)
        {
            stopEvent = stop;
            device = new LenovoBacklightDevice();
            int initial = device.ReadLevel();
            resumePending = File.Exists(Settings.FilePath);
            settings = Settings.Load(initial);
            // Save the initial level too, so a restart during suppression can restore it.
            settings.Save();
            policy = new BacklightPolicy(device, settings.PreferredLevel, settings.AutoEnabled, delegate(int level)
            {
                settings.PreferredLevel = level;
                settings.Save();
            });
            power = new PowerWindow();
            wasActive = power.IsActive;
            power.Resuming += delegate { resumePending = true; settleUntil = DateTime.UtcNow.AddMilliseconds(1400); };
            power.SessionEnding += delegate { ExitThread(); };
            menu = new ContextMenuStrip();
            ToolStripMenuItem title = new ToolStripMenuItem("L14 Keyboard Light");
            title.Enabled = false;
            menu.Items.Add(title);
            menu.Items.Add(new ToolStripSeparator());
            string[] names = { "Ki", "Halvány", "Erős" };
            for (int index = 0; index < 3; index++)
            {
                int chosen = index;
                levels[index] = new ToolStripMenuItem(names[index]);
                levels[index].Click += delegate { Act(delegate { policy.ChooseLevel(chosen); }); };
                menu.Items.Add(levels[index]);
            }
            menu.Items.Add(new ToolStripSeparator());
            automatic = new ToolStripMenuItem("Teljes képernyőn automatikusan kikapcsol");
            automatic.Click += delegate
            {
                Act(delegate
                {
                    policy.SetAutoEnabled(!policy.AutoEnabled);
                    settings.AutoEnabled = policy.AutoEnabled;
                    settings.Save();
                });
            };
            menu.Items.Add(automatic);
            startup = new ToolStripMenuItem("Indítás a Windowszal");
            startup.Click += delegate { Act(delegate { SetStartup(!HasStartup()); }); };
            menu.Items.Add(startup);
            ToolStripMenuItem status = new ToolStripMenuItem("Állapot…");
            status.Click += delegate
            {
                string text = "Gép: " + device.Family + " (" + device.Model + ")\r\n" +
                    "Kívánt fényerő: " + names[policy.PreferredLevel] + "\r\n" +
                    "Teljes képernyő: " + (policy.IsFullscreen ? "igen" : "nem") + "\r\n" +
                    "Automatikusan lekapcsolva: " + (policy.IsSuppressed ? "igen" : "nem") + "\r\n" +
                    "Kézi felülbírálás: " + (policy.ManualOverride ? "igen" : "nem") + "\r\n" +
                    "Automatikus indulás: " + (HasStartup() ? "be" : "ki") + "\r\n" +
                    (lastError == null ? "Állapot: rendben" : "Utolsó hiba: " + lastError);
                MessageBox.Show(text, "L14 Keyboard Light", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            menu.Items.Add(status);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exit = new ToolStripMenuItem("Kilépés");
            exit.Click += delegate { ExitThread(); };
            menu.Items.Add(exit);
            menu.Opening += delegate { UpdateMenu(); };
            icon = MakeIcon();
            tray = new NotifyIcon { Icon = icon, Text = "L14 Keyboard Light", ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += delegate { status.PerformClick(); };
            timer = new System.Windows.Forms.Timer { Interval = 350 };
            timer.Tick += Tick;
            timer.Start();
            Application.ApplicationExit += ApplicationExiting;
            Settings.Log("Started 1.0.0; model=" + device.Model + "; initial=" + initial + "; preferred=" + policy.PreferredLevel);
            UpdateMenu();
        }

        private void Tick(object sender, EventArgs e)
        {
            if (stopEvent.WaitOne(0)) { ExitThread(); return; }
            bool active = power.IsActive;
            if (active && !wasActive) { resumePending = true; settleUntil = DateTime.UtcNow.AddMilliseconds(1400); }
            wasActive = active;
            if (!active || DateTime.UtcNow < settleUntil)
            {
                policy.Poll(false, false, false);
                return;
            }
            if (DateTime.UtcNow < retryAfter) return;
            try
            {
                policy.Poll(FullscreenDetector.IsForegroundFullscreen(), true, resumePending);
                resumePending = false;
                lastError = null;
                string state = "fullscreen=" + policy.IsFullscreen + "; suppressed=" + policy.IsSuppressed +
                    "; override=" + policy.ManualOverride + "; preferred=" + policy.PreferredLevel;
                if (state != lastState) { Settings.Log(state); lastState = state; }
                tray.Text = policy.IsSuppressed ? "L14 Keyboard Light – teljes képernyő: Ki" : "L14 Keyboard Light";
            }
            catch (Exception error) { ReportError(error); }
        }

        private void Act(Action action)
        {
            try
            {
                if (!power.IsActive) return;
                action();
                lastError = null;
                retryAfter = DateTime.MinValue;
                UpdateMenu();
            }
            catch (Exception error) { ReportError(error); }
        }

        private void ReportError(Exception error)
        {
            retryAfter = DateTime.UtcNow.AddSeconds(5);
            if (error.Message != lastError)
            {
                Settings.Log(error.ToString());
                tray.ShowBalloonTip(7000, "L14 Keyboard Light", error.Message, ToolTipIcon.Warning);
            }
            lastError = error.Message;
            tray.Text = "L14 Keyboard Light – vezérlési hiba";
        }

        private void UpdateMenu()
        {
            for (int index = 0; index < levels.Length; index++) levels[index].Checked = policy.PreferredLevel == index;
            automatic.Checked = policy.AutoEnabled;
            startup.Checked = HasStartup();
        }

        private static bool HasStartup()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                return key != null && String.Equals(Convert.ToString(key.GetValue("L14KeyboardLight")), "\"" + Application.ExecutablePath + "\"", StringComparison.OrdinalIgnoreCase);
        }

        private static void SetStartup(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (enable) key.SetValue("L14KeyboardLight", "\"" + Application.ExecutablePath + "\"");
                else key.DeleteValue("L14KeyboardLight", false);
            }
        }

        private static Icon MakeIcon()
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (Pen outline = new Pen(Color.FromArgb(240, 240, 240), 2))
            using (Brush light = new SolidBrush(Color.FromArgb(255, 185, 75)))
            {
                graphics.Clear(Color.Transparent);
                graphics.DrawRectangle(outline, 2, 11, 27, 16);
                for (int row = 0; row < 2; row++)
                    for (int column = 0; column < 5; column++) graphics.FillRectangle(light, 5 + column * 5, 14 + row * 5, 3, 3);
                graphics.DrawLine(outline, 8, 6, 8, 3);
                graphics.DrawLine(outline, 16, 7, 16, 2);
                graphics.DrawLine(outline, 24, 6, 24, 3);
                IntPtr handle = bitmap.GetHicon();
                try { using (Icon temporary = Icon.FromHandle(handle)) return (Icon)temporary.Clone(); }
                finally { DestroyIcon(handle); }
            }
        }

        private void ApplicationExiting(object sender, EventArgs e) { Restore(); }

        private void Restore()
        {
            if (exiting) return;
            exiting = true;
            timer.Stop();
            if (power.IsActive)
            {
                // Short retries cover a transient device failure while preserving ownership.
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    try { policy.Shutdown(); break; }
                    catch (Exception error) { Settings.Log("Exit restore: " + error.Message); Thread.Sleep(150); }
                }
            }
            Settings.Log("Stopped; preferred=" + policy.PreferredLevel);
            tray.Visible = false;
        }

        protected override void ExitThreadCore() { Restore(); base.ExitThreadCore(); }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Restore();
                Application.ApplicationExit -= ApplicationExiting;
                timer.Dispose(); tray.Dispose(); menu.Dispose(); icon.Dispose(); power.Dispose();
            }
            base.Dispose(disposing);
        }
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);
    }
}
