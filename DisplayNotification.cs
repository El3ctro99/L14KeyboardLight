using System;
using System.Runtime.InteropServices;

namespace L14KeyboardLight
{
    public static class DisplayNotification
    {
        public static readonly Guid SessionDisplayStatus =
            new Guid("2b84c20e-ad23-4ddf-93db-05ffbd7efca5");

        // packet is the POWERBROADCAST_SETTING pointer supplied by Windows.
        // Data follows the 16-byte GUID and the four-byte DataLength field;
        // PtrToStructure must not include the structure's variable-sized tail.
        public static bool TryRead(IntPtr packet, out bool displayOn)
        {
            displayOn = false;
            if (packet == IntPtr.Zero)
                return false;

            Guid setting = (Guid)Marshal.PtrToStructure(packet, typeof(Guid));
            if (setting != SessionDisplayStatus || Marshal.ReadInt32(packet, 16) != 4)
                return false;

            int state = Marshal.ReadInt32(packet, 20);
            if (state < 0 || state > 2)
                return false;

            // A dimmed screen is still on. Only state zero suppresses the light.
            displayOn = state != 0;
            return true;
        }
    }
}
