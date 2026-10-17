// Development-only integration test. This file is not part of the tray application.
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using L14KeyboardLight;

internal static class LiveSmokeTest
{
    [STAThread]
    private static int Main(string[] args)
    {
        SetProcessDpiAwarenessContext(new IntPtr(-4));
        Application.EnableVisualStyles();
        using (ProbeForm form = new ProbeForm(args[0])) { Application.Run(form); return form.Result; }
    }
    [DllImport("user32.dll")] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
}

internal sealed class ProbeForm : Form
{
    private readonly string report;
    private readonly LenovoBacklightDevice device = new LenovoBacklightDevice();
    private readonly IntPtr previousForeground;
    private readonly Label label;
    private int original;
    public int Result = 1;

    public ProbeForm(string reportPath)
    {
        report = reportPath;
        previousForeground = GetForegroundWindow();
        Text = "L14 Keyboard Light – működési próba";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(700, 340);
        BackColor = Color.FromArgb(28, 30, 34);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 16);
        label = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
        Controls.Add(label);
        TopMost = true;
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        original = device.ReadLevel();
        int baseline = original == 0 ? 1 : original;
        int manual = baseline == 1 ? 2 : 1;
        try
        {
            File.WriteAllText(report, "Live L14 integration test\r\nInitial level: " + original + "\r\n");
            device.WriteLevel(baseline);
            label.Text = "Billentyűzetvilágítás próba\r\nRövidesen teljes képernyőre vált…";
            await Task.Delay(1300);
            CheckLevel(baseline, "Ordinary window preserves level");
            Fullscreen();
            label.Text = "Teljes képernyő\r\nA billentyűzetfénynek most ki kell kapcsolnia.";
            await Task.Delay(1600);
            CheckForeground(true);
            CheckLevel(0, "Fullscreen switches the actual backlight Off");
            Ordinary();
            label.Text = "Kilépés a teljes képernyőből\r\nA korábbi fényerő visszaáll.";
            await Task.Delay(1400);
            CheckForeground(false);
            CheckLevel(baseline, "Leaving fullscreen restores the actual prior level");
            Fullscreen();
            label.Text = "Kézi felülbírálás ellenőrzése";
            await Task.Delay(1300);
            CheckLevel(0, "A new fullscreen session suppresses again");
            device.WriteLevel(manual);
            await Task.Delay(1400);
            CheckLevel(manual, "Manual hardware choice survives while still fullscreen");
            Ordinary();
            await Task.Delay(1200);
            CheckLevel(manual, "Leaving fullscreen keeps the manual override");
            WindowState = FormWindowState.Maximized;
            Activate();
            await Task.Delay(1200);
            CheckForeground(false);
            CheckLevel(manual, "An ordinary maximized window is not suppressed");
            Ordinary();
            device.WriteLevel(baseline);
            await Task.Delay(1100);
            Fullscreen();
            label.Text = "Szabályos leállítás és fényerő-visszaállítás ellenőrzése";
            await Task.Delay(1300);
            CheckLevel(0, "Fullscreen before clean exit is Off");
            using (EventWaitHandle stop = EventWaitHandle.OpenExisting(@"Local\L14KeyboardLight-Stop-v1")) stop.Set();
            await Task.Delay(1300);
            CheckLevel(baseline, "Clean app exit restores its owned suppression");
            Result = 0;
            File.AppendAllText(report, "OVERALL: PASS\r\n");
        }
        catch (Exception error)
        {
            File.AppendAllText(report, "FAIL: " + error + "\r\n");
        }
        finally
        {
            try
            {
                Ordinary();
                device.WriteLevel(original);
                Thread.Sleep(800);
                Settings saved = Settings.Load(original);
                saved.PreferredLevel = original;
                saved.Save();
            }
            catch (Exception error) { File.AppendAllText(report, "Final restoration error: " + error.Message + "\r\n"); Result = 1; }
            Close();
            if (previousForeground != IntPtr.Zero) SetForegroundWindow(previousForeground);
        }
    }

    private void CheckLevel(int expected, string name)
    {
        int actual = device.ReadLevel();
        if (actual != expected) throw new Exception(name + ": expected=" + expected + ", actual=" + actual);
        File.AppendAllText(report, "PASS: " + name + " (" + actual + ")\r\n");
    }
    private void CheckForeground(bool expected)
    {
        if (GetForegroundWindow() != Handle) throw new Exception("The test window lost foreground; fullscreen test is inconclusive. " + FullscreenDetector.DescribeForeground());
        if (FullscreenDetector.IsForegroundFullscreen() != expected) throw new Exception("Native fullscreen detection mismatch: " + FullscreenDetector.DescribeForeground());
        File.AppendAllText(report, "PASS: native foreground detection=" + expected + "\r\n");
    }
    private void Fullscreen()
    {
        WindowState = FormWindowState.Normal;
        Rectangle monitor = Screen.FromHandle(Handle).Bounds;
        FormBorderStyle = FormBorderStyle.None;
        Bounds = monitor;
        Activate();
        SetForegroundWindow(Handle);
    }
    private void Ordinary()
    {
        WindowState = FormWindowState.Normal;
        FormBorderStyle = FormBorderStyle.Sizable;
        Rectangle work = Screen.FromHandle(Handle).WorkingArea;
        Bounds = new Rectangle(work.X + 120, work.Y + 120, 700, 340);
        Activate();
        SetForegroundWindow(Handle);
    }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
}
