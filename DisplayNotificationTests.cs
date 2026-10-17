using System;
using System.IO;
using System.Runtime.InteropServices;

namespace L14KeyboardLight
{
    public static class DisplayNotificationTests
    {
        public static int Run(TextWriter output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            int passed = 0;
            int failed = 0;
            Check(output, "Null notification is rejected", delegate
            {
                bool displayOn = true;
                Assert(!DisplayNotification.TryRead(IntPtr.Zero, out displayOn), "null accepted");
                Assert(!displayOn, "invalid result did not clear output");
            }, ref passed, ref failed);

            CheckPacket(output, "Display Off", DisplayNotification.SessionDisplayStatus, 4, 0,
                true, false, ref passed, ref failed);
            CheckPacket(output, "Display On", DisplayNotification.SessionDisplayStatus, 4, 1,
                true, true, ref passed, ref failed);
            CheckPacket(output, "Dimmed display remains On", DisplayNotification.SessionDisplayStatus, 4, 2,
                true, true, ref passed, ref failed);
            CheckPacket(output, "Unknown state three", DisplayNotification.SessionDisplayStatus, 4, 3,
                false, false, ref passed, ref failed);
            CheckPacket(output, "Invalid unsigned state", DisplayNotification.SessionDisplayStatus, 4, -1,
                false, false, ref passed, ref failed);
            CheckPacket(output, "Invalid large state", DisplayNotification.SessionDisplayStatus, 4, Int32.MaxValue,
                false, false, ref passed, ref failed);

            int[] lengths = new int[] { 0, 1, 3, 5, 16, Int32.MinValue, -1 };
            foreach (int length in lengths)
            {
                int packetLength = length;
                Check(output, "Reject DataLength " + packetLength, delegate
                {
                    // A header-only allocation also checks that invalid lengths
                    // are rejected before the variable data field is read.
                    IntPtr packet = AllocateHeader(DisplayNotification.SessionDisplayStatus, packetLength);
                    try
                    {
                        bool displayOn = true;
                        Assert(!DisplayNotification.TryRead(packet, out displayOn), "bad length accepted");
                        Assert(!displayOn, "invalid result did not clear output");
                    }
                    finally { Marshal.FreeHGlobal(packet); }
                }, ref passed, ref failed);
            }

            Check(output, "Other power setting GUID is rejected", delegate
            {
                IntPtr packet = AllocateHeader(new Guid("6fe69556-704a-47a0-8f24-c28d936fda47"), 4);
                try
                {
                    bool displayOn = true;
                    Assert(!DisplayNotification.TryRead(packet, out displayOn), "wrong GUID accepted");
                    Assert(!displayOn, "invalid result did not clear output");
                }
                finally { Marshal.FreeHGlobal(packet); }
            }, ref passed, ref failed);

            Check(output, "GUID constant and wire offsets match Windows layout", delegate
            {
                Guid expected = new Guid("2b84c20e-ad23-4ddf-93db-05ffbd7efca5");
                Assert(DisplayNotification.SessionDisplayStatus == expected, "unexpected setting GUID");
                Assert(Marshal.SizeOf(typeof(Guid)) == 16, "GUID size");
                IntPtr packet = Marshal.AllocHGlobal(24);
                try
                {
                    byte[] wire = new byte[]
                    {
                        0x0e, 0xc2, 0x84, 0x2b, 0x23, 0xad, 0xdf, 0x4d,
                        0x93, 0xdb, 0x05, 0xff, 0xbd, 0x7e, 0xfc, 0xa5,
                        0x04, 0x00, 0x00, 0x00,
                        0x01, 0x00, 0x00, 0x00
                    };
                    Marshal.Copy(wire, 0, packet, wire.Length);
                    bool displayOn;
                    Assert(DisplayNotification.TryRead(packet, out displayOn), "literal Windows packet rejected");
                    Assert(displayOn, "literal On packet decoded as Off");
                }
                finally { Marshal.FreeHGlobal(packet); }
            }, ref passed, ref failed);

            output.WriteLine("Display notification tests: " + passed + " passed, " + failed + " failed.");
            return failed == 0 ? 0 : 1;
        }

        private static IntPtr AllocateHeader(Guid setting, int dataLength)
        {
            IntPtr packet = Marshal.AllocHGlobal(20);
            Marshal.Copy(setting.ToByteArray(), 0, packet, 16);
            Marshal.WriteInt32(packet, 16, dataLength);
            return packet;
        }

        private static void CheckPacket(TextWriter output, string name, Guid setting, int dataLength,
            int state, bool expectedValid, bool expectedOn, ref int passed, ref int failed)
        {
            Check(output, name, delegate
            {
                IntPtr packet = Marshal.AllocHGlobal(24);
                try
                {
                    Marshal.Copy(setting.ToByteArray(), 0, packet, 16);
                    Marshal.WriteInt32(packet, 16, dataLength);
                    Marshal.WriteInt32(packet, 20, state);
                    byte[] before = new byte[24];
                    Marshal.Copy(packet, before, 0, before.Length);
                    bool displayOn = !expectedOn;
                    bool valid = DisplayNotification.TryRead(packet, out displayOn);
                    Assert(valid == expectedValid, "validity mismatch");
                    Assert(displayOn == expectedOn, "display state mismatch");
                    byte[] after = new byte[24];
                    Marshal.Copy(packet, after, 0, after.Length);
                    for (int i = 0; i < before.Length; i++)
                        Assert(before[i] == after[i], "notification was modified at byte " + i);
                }
                finally { Marshal.FreeHGlobal(packet); }
            }, ref passed, ref failed);
        }

        private static void Check(TextWriter output, string name, Action action, ref int passed, ref int failed)
        {
            try
            {
                action();
                passed++;
                output.WriteLine("PASS: " + name);
            }
            catch (Exception ex)
            {
                failed++;
                output.WriteLine("FAIL: " + name + ": " + ex.Message);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
