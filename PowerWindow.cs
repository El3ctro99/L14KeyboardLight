using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace L14KeyboardLight
{
    internal sealed class PowerWindow : Form
    {
        private IntPtr notification;
        private bool sessionRegistered;
        public bool Locked;
        public bool Disconnected;
        public bool Suspended;
        public bool DisplayOn = true;
        public event EventHandler Resuming;
        public event EventHandler DisplayStateChanged;
        public event EventHandler SessionEnding;

        public PowerWindow()
        {
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Text = "L14 Keyboard Light power listener";
            IntPtr window = Handle; // A hidden top-level HWND receives broadcast power messages.
            sessionRegistered = WTSRegisterSessionNotification(window, 0);
            Guid display = DisplayNotification.SessionDisplayStatus;
            notification = RegisterPowerSettingNotification(window, ref display, 0);
            if (!sessionRegistered || notification == IntPtr.Zero)
                throw UiText.WithMessage(new InvalidOperationException(UiText.Get("ErrorPowerEvents")), "ErrorPowerEvents");
        }

        public bool IsActive
        {
            get { return !Locked && !Disconnected && !Suspended && DisplayOn && IsDefaultInputDesktop(); }
        }

        public bool CanControlBacklight
        {
            get { return !Suspended && !Disconnected; }
        }

        private void OnResuming()
        {
            EventHandler handler = Resuming;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected override void WndProc(ref Message message)
        {
            const int WM_POWERBROADCAST = 0x218;
            const int WM_WTSSESSION_CHANGE = 0x2B1;
            if (message.Msg == 0x16 && message.WParam != IntPtr.Zero) // WM_ENDSESSION, committed logoff/shutdown
            {
                EventHandler ending = SessionEnding;
                if (ending != null) ending(this, EventArgs.Empty);
            }
            else if (message.Msg == WM_POWERBROADCAST)
            {
                int reason = message.WParam.ToInt32();
                if (reason == 4) Suspended = true;
                else if (reason == 7 || reason == 0x12)
                {
                    Suspended = false;
                    OnResuming();
                }
                else if (reason == 0x8013)
                {
                    bool displayOn;
                    if (DisplayNotification.TryRead(message.LParam, out displayOn))
                    {
                        bool wasOn = DisplayOn;
                        DisplayOn = displayOn;
                        if (DisplayOn != wasOn)
                        {
                            EventHandler changed = DisplayStateChanged;
                            if (changed != null) changed(this, EventArgs.Empty);
                        }
                        if (DisplayOn && !wasOn) OnResuming();
                    }
                }
            }
            else if (message.Msg == WM_WTSSESSION_CHANGE)
            {
                int reason = message.WParam.ToInt32();
                if (reason == 7) Locked = true;
                else if (reason == 8) { Locked = false; OnResuming(); }
                else if (reason == 2 || reason == 4 || reason == 6) Disconnected = true;
                else if (reason == 1 || reason == 3 || reason == 5) { Disconnected = false; OnResuming(); }
            }
            base.WndProc(ref message);
        }

        private static bool IsDefaultInputDesktop()
        {
            IntPtr desktop = OpenInputDesktop(0, false, 1);
            if (desktop == IntPtr.Zero) return false;
            try
            {
                StringBuilder name = new StringBuilder(256);
                uint needed;
                return GetUserObjectInformation(desktop, 2, name, 512, out needed) && name.ToString().Equals("Default", StringComparison.OrdinalIgnoreCase);
            }
            finally { CloseDesktop(desktop); }
        }

        protected override void Dispose(bool disposing)
        {
            if (notification != IntPtr.Zero) { UnregisterPowerSettingNotification(notification); notification = IntPtr.Zero; }
            if (sessionRegistered) { WTSUnRegisterSessionNotification(Handle); sessionRegistered = false; }
            base.Dispose(disposing);
        }

        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr RegisterPowerSettingNotification(IntPtr hwnd, ref Guid setting, uint flags);
        [DllImport("user32.dll")] private static extern bool UnregisterPowerSettingNotification(IntPtr handle);
        [DllImport("wtsapi32.dll", SetLastError = true)] private static extern bool WTSRegisterSessionNotification(IntPtr hwnd, uint flags);
        [DllImport("wtsapi32.dll")] private static extern bool WTSUnRegisterSessionNotification(IntPtr hwnd);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr OpenInputDesktop(uint flags, bool inherit, uint access);
        [DllImport("user32.dll")] private static extern bool CloseDesktop(IntPtr desktop);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool GetUserObjectInformation(IntPtr handle, int index, StringBuilder info, uint length, out uint needed);
    }
}
