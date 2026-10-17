using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace L14KeyboardLight
{
    // These values were read and round-trip tested on the user's 21C2 L14 Gen 3.
    // Never fall back to direct EC/memory access or a third-party kernel driver.
    public sealed class LenovoBacklightDevice : IBacklightDevice
    {
        private const uint GetIoctl = 0x00222680;
        private const uint SetIoctl = 0x00222684;
        public string Model { get; private set; }
        public string Family { get; private set; }

        public LenovoBacklightDevice()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS"))
            {
                if (key == null) throw new InvalidOperationException("Nem olvasható a gép típusa.");
                Model = Convert.ToString(key.GetValue("SystemProductName"));
                Family = Convert.ToString(key.GetValue("SystemFamily"));
                string maker = Convert.ToString(key.GetValue("SystemManufacturer"));
                if (!maker.Equals("LENOVO", StringComparison.OrdinalIgnoreCase) ||
                    !Model.StartsWith("21C2", StringComparison.OrdinalIgnoreCase) ||
                    Family.IndexOf("ThinkPad L14 Gen 3", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new NotSupportedException("Ez a kiadás csak az ellenőrzött ThinkPad L14 Gen 3 (21C2) típushoz készült.");
            }
        }

        private static SafeFileHandle OpenDevice()
        {
            // Lenovo's device expects FileAccess.ReadWrite (3), not GENERIC_*.
            SafeFileHandle handle = CreateFile(@"\\.\IBMPmDrv", 3, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                handle.Dispose();
                throw new Win32Exception(error, "Nem érhető el a Lenovo energiagazdálkodási drivere (IBMPmDrv).");
            }
            return handle;
        }

        public int ReadLevel()
        {
            using (SafeFileHandle handle = OpenDevice())
            {
                byte[] output = new byte[16];
                uint count;
                if (!DeviceIoControl(handle, GetIoctl, null, 0, output, (uint)output.Length, out count, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Nem sikerült kiolvasni a billentyűzet fényerejét.");
                if (count < 4) throw new InvalidOperationException("A Lenovo-driver hiányos választ adott.");
                uint value = BitConverter.ToUInt32(output, 0);
                if (value < 0x00250200 || value > 0x00250202)
                    throw new NotSupportedException("Ismeretlen Lenovo-válasz: 0x" + value.ToString("X8") + ". A vezérlés leállt.");
                return (int)(value - 0x00250200);
            }
        }

        public void WriteLevel(int level)
        {
            if (level < 0 || level > 2) throw new ArgumentOutOfRangeException("level");
            using (SafeFileHandle handle = OpenDevice())
            {
                byte[] input = BitConverter.GetBytes((uint)level);
                byte[] output = new byte[16];
                uint count;
                if (!DeviceIoControl(handle, SetIoctl, input, (uint)input.Length, output, (uint)output.Length, out count, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Nem sikerült beállítani a billentyűzet fényerejét.");
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeviceIoControl(SafeFileHandle device, uint code, byte[] input, uint inputSize, [Out] byte[] output, uint outputSize, out uint returned, IntPtr overlapped);
    }
}
