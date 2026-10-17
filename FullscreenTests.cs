using System;
using System.Drawing;
using System.IO;

namespace L14KeyboardLight
{
    public static class FullscreenTests
    {
        public static int Run(TextWriter output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            int failures = 0;
            int total = 0;
            Rectangle primary = new Rectangle(0, 0, 1920, 1080);
            Rectangle left = new Rectangle(-2560, -300, 2560, 1440);
            Rectangle above = new Rectangle(320, -2160, 3840, 2160);

            Check(output, ref total, ref failures, "Exact primary monitor", true, primary, primary);
            Check(output, ref total, ref failures, "Secondary monitor with negative coordinates", true, left, left);
            Check(output, ref total, ref failures, "Monitor above primary", true, above, above);
            Check(output, ref total, ref failures, "Work area leaves bottom taskbar", false,
                new Rectangle(0, 0, 1920, 1040), primary);
            Check(output, ref total, ref failures, "Work area leaves left taskbar", false,
                new Rectangle(48, 0, 1872, 1080), primary);
            Check(output, ref total, ref failures, "Full bounds include taskbar area", true, primary, primary);
            Check(output, ref total, ref failures, "Same size at wrong origin", false,
                new Rectangle(100, 0, 1920, 1080), primary);
            Check(output, ref total, ref failures, "Primary bounds do not match secondary", false, primary, left);
            Check(output, ref total, ref failures, "Spanning two monitors", false,
                new Rectangle(0, 0, 3840, 1080), primary);
            Check(output, ref total, ref failures, "Oversized containing window", false,
                new Rectangle(-20, -20, 1960, 1120), primary);
            Check(output, ref total, ref failures, "Two pixel inset on all edges", true,
                new Rectangle(2, 2, 1916, 1076), primary);
            Check(output, ref total, ref failures, "Two pixel outset on all edges", true,
                new Rectangle(-2, -2, 1924, 1084), primary);
            Check(output, ref total, ref failures, "Two pixel offset preserves dimensions", true,
                new Rectangle(2, -2, 1920, 1080), primary);
            Check(output, ref total, ref failures, "Three pixel left edge rejected", false,
                new Rectangle(3, 0, 1917, 1080), primary);
            Check(output, ref total, ref failures, "Three pixel right edge rejected", false,
                new Rectangle(0, 0, 1923, 1080), primary);
            Check(output, ref total, ref failures, "Three pixel top edge rejected", false,
                new Rectangle(0, -3, 1920, 1083), primary);
            Check(output, ref total, ref failures, "Three pixel bottom edge rejected", false,
                new Rectangle(0, 0, 1920, 1077), primary);
            Check(output, ref total, ref failures, "Normal maximized captioned window", false,
                primary, primary, true, false, false, false, true, true);
            Check(output, ref total, ref failures, "Borderless maximized fullscreen", true,
                primary, primary, true, false, false, false, true, false);
            Check(output, ref total, ref failures, "Unmaximized caption at full monitor bounds", true,
                primary, primary, true, false, false, false, false, true);
            Check(output, ref total, ref failures, "Invisible window", false,
                primary, primary, false, false, false, false, false, false);
            Check(output, ref total, ref failures, "Minimized window", false,
                primary, primary, true, true, false, false, false, false);
            Check(output, ref total, ref failures, "Cloaked window", false,
                primary, primary, true, false, true, false, false, false);
            Check(output, ref total, ref failures, "Desktop taskbar or lock screen", false,
                primary, primary, true, false, false, true, false, false);
            Check(output, ref total, ref failures, "Empty window", false, Rectangle.Empty, primary);
            Check(output, ref total, ref failures, "Empty monitor", false, primary, Rectangle.Empty);
            Check(output, ref total, ref failures, "Negative window width", false,
                new Rectangle(0, 0, -1920, 1080), primary);
            Check(output, ref total, ref failures, "Negative monitor height", false,
                primary, new Rectangle(0, 0, 1920, -1080));
            Rectangle extreme = new Rectangle(Int32.MaxValue - 100, Int32.MinValue + 100, 200, 1000);
            Check(output, ref total, ref failures, "Extreme virtual coordinates avoid edge overflow", true,
                extreme, extreme);
            Check(output, ref total, ref failures, "Extreme virtual coordinates still reject offset", false,
                new Rectangle(Int32.MaxValue - 97, Int32.MinValue + 100, 197, 1000), extreme);

            output.WriteLine("Fullscreen geometry: {0}/{1} passed.", total - failures, total);
            return failures;
        }

        private static void Check(TextWriter output, ref int total, ref int failures,
            string name, bool expected, Rectangle window, Rectangle monitor)
        {
            Check(output, ref total, ref failures, name, expected, window, monitor,
                true, false, false, false, false, false);
        }

        private static void Check(TextWriter output, ref int total, ref int failures,
            string name, bool expected, Rectangle window, Rectangle monitor,
            bool visible, bool minimized, bool cloaked, bool shell, bool zoomed, bool hasCaption)
        {
            total++;
            bool actual = FullscreenDetector.IsFullscreenGeometry(window, monitor,
                visible, minimized, cloaked, shell, zoomed, hasCaption);
            if (actual != expected)
            {
                failures++;
                output.WriteLine("FAIL {0}: expected {1}, actual {2}.", name, expected, actual);
            }
        }
    }
}
