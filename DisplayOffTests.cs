using System;
using System.Collections.Generic;
using System.IO;

namespace L14KeyboardLight
{
    public static class DisplayOffTests
    {
        public static int Run(TextWriter output)
        {
            if (output == null) throw new ArgumentNullException("output");
            int passed = 0;
            int failed = 0;

            RunOne(output, "Display off restores every preferred brightness without saving forced Off", delegate
            {
                for (int level = 0; level <= 2; level++)
                {
                    Fixture f = new Fixture(level, true);
                    f.Policy.SuppressForDisplayOff();
                    Equal(0, f.Device.Level, "screen-off hardware");
                    True(f.Policy.IsDisplayOff, "display-off latch");
                    True(f.Policy.IsSuppressed, "screen-off ownership");
                    f.Policy.SuppressForDisplayOff();
                    Equal(level == 0 ? 0 : 1, f.Device.Writes.Count, "no redundant suppression writes");
                    f.Policy.Poll(false, true, true);
                    Equal(level, f.Device.Level, "screen-on restored brightness");
                    Equal(level, f.Policy.PreferredLevel, "preference preserved");
                    Equal(0, f.Saves.Count, "forced Off never saved");
                    False(f.Policy.IsDisplayOff, "screen-on releases display latch");
                    False(f.Policy.IsSuppressed, "screen-on releases ownership");
                }
            }, ref passed, ref failed);

            RunOne(output, "Display off works when fullscreen automation is disabled", delegate
            {
                Fixture f = new Fixture(2, false);
                f.Policy.Poll(true, true, false);
                Equal(2, f.Device.Level, "fullscreen automation remains disabled");
                f.Policy.SuppressForDisplayOff();
                Equal(0, f.Device.Level, "display-off suppression independent of fullscreen setting");
                f.Policy.Poll(true, true, true);
                Equal(2, f.Device.Level, "display-on restores with automation disabled");
                False(f.Policy.AutoEnabled, "fullscreen setting unchanged");
            }, ref passed, ref failed);

            RunOne(output, "Fullscreen snapshot survives display off and wake into fullscreen", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Policy.Poll(true, true, false);
                f.Policy.SuppressForDisplayOff();
                f.Policy.Poll(false, false, false);
                f.Device.Level = 1;
                f.Policy.Poll(true, true, true);
                Equal(0, f.Device.Level, "fullscreen suppression remains after wake");
                False(f.Policy.ManualOverride, "firmware default is not a user choice");
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "original fullscreen snapshot restored");
                Equal(0, f.Saves.Count, "neither temporary value saved");
            }, ref passed, ref failed);

            RunOne(output, "Fullscreen snapshot restores when display wakes outside fullscreen", delegate
            {
                Fixture f = new Fixture(1, true);
                f.Policy.Poll(true, true, false);
                f.Policy.SuppressForDisplayOff();
                f.Policy.Poll(false, true, true);
                Equal(1, f.Device.Level, "original snapshot restored outside fullscreen");
                False(f.Policy.IsSuppressed, "ownership released");
                Equal(0, f.Saves.Count, "no forced preference save");
            }, ref passed, ref failed);

            RunOne(output, "Manual fullscreen hardware override survives display off in the same fullscreen session", delegate
            {
                Fixture f = new Fixture(1, true);
                f.Policy.Poll(true, true, false);
                f.Device.Level = 2;
                f.Policy.Poll(true, true, false);
                True(f.Policy.ManualOverride, "hardware override recorded");
                f.Policy.SuppressForDisplayOff();
                Equal(0, f.Device.Level, "display-off still suppresses a manual override");
                f.Policy.Poll(false, false, false);
                True(f.Policy.IsFullscreen, "inactive polls retain fullscreen state");
                True(f.Policy.ManualOverride, "inactive polls retain manual override");
                f.Policy.Poll(true, true, true);
                Equal(2, f.Device.Level, "manual level restored in same fullscreen session");
                True(f.Policy.ManualOverride, "manual override preserved through wake");
                f.Policy.Poll(true, true, false);
                Equal(2, f.Device.Level, "next poll respects preserved override");
                f.Policy.Poll(false, true, false);
                f.Policy.Poll(true, true, false);
                Equal(0, f.Device.Level, "new fullscreen session suppresses again");
                Sequence(new int[] { 2 }, f.Saves, "only explicit hardware preference saved");
            }, ref passed, ref failed);

            RunOne(output, "Display off snapshots preference when firmware has already turned the light off", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Device.Level = 0;
                f.Policy.SuppressForDisplayOff();
                f.Policy.Poll(false, true, true);
                Equal(2, f.Device.Level, "known preferred brightness restored");
                Equal(0, f.Saves.Count, "firmware Off not saved");
            }, ref passed, ref failed);

            RunOne(output, "Display wake replaces a firmware nonzero default with the remembered preference", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Policy.SuppressForDisplayOff();
                f.Device.Level = 1;
                f.Policy.Poll(false, true, true);
                Equal(2, f.Device.Level, "firmware low reset replaced by preference");
                Equal(2, f.Policy.PreferredLevel, "firmware low never becomes preference");
                Equal(0, f.Saves.Count, "wake default not saved");
            }, ref passed, ref failed);

            RunOne(output, "An explicit Off preference remains Off after a firmware wake default", delegate
            {
                Fixture f = new Fixture(0, true);
                f.Policy.SuppressForDisplayOff();
                f.Device.Level = 2;
                f.Policy.Poll(false, true, true);
                Equal(0, f.Device.Level, "remembered Off reapplied");
                Equal(0, f.Policy.PreferredLevel, "Off preference retained");
                Equal(0, f.Saves.Count, "no automatic preference saves");
            }, ref passed, ref failed);

            RunOne(output, "A locked or suspended session cannot restore display suppression", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Policy.SuppressForDisplayOff();
                int reads = f.Device.Reads;
                int writes = f.Device.WriteAttempts;
                for (int i = 0; i < 3; i++) f.Policy.Poll(false, false, true);
                Equal(reads, f.Device.Reads, "inactive reads");
                Equal(writes, f.Device.WriteAttempts, "inactive writes");
                Equal(0, f.Device.Level, "inactive display-on remains Off");
                True(f.Policy.IsDisplayOff, "display latch retained until active");
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "active desktop finally restores");
            }, ref passed, ref failed);

            RunOne(output, "A failed display-off read retains the preferred snapshot for retry", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Device.ReadFailures = 1;
                IoFailure(delegate { f.Policy.SuppressForDisplayOff(); });
                True(f.Policy.IsDisplayOff, "off latch set before failed read");
                False(f.Policy.IsSuppressed, "failed read does not claim completed suppression");
                f.Device.Level = 0;
                f.Policy.SuppressForDisplayOff();
                f.Policy.Poll(false, true, true);
                Equal(2, f.Device.Level, "read retry preserves preferred snapshot");
                Equal(0, f.Saves.Count, "failed read does not save temporary Off");
            }, ref passed, ref failed);

            RunOne(output, "Failed display-off writes retry whether failure precedes or follows hardware change", delegate
            {
                for (int after = 0; after <= 1; after++)
                {
                    Fixture f = new Fixture(2, true);
                    f.Device.WriteFailuresBefore = after == 0 ? 1 : 0;
                    f.Device.WriteFailuresAfter = after == 1 ? 1 : 0;
                    IoFailure(delegate { f.Policy.SuppressForDisplayOff(); });
                    False(f.Policy.IsSuppressed, "uncertain suppression not yet marked successful");
                    f.Policy.Poll(false, false, false);
                    f.Policy.SuppressForDisplayOff();
                    Equal(0, f.Device.Level, "suppression retry");
                    f.Policy.Poll(false, true, true);
                    Equal(2, f.Device.Level, "failed write did not lose restoration");
                    Equal(0, f.Saves.Count, "failed write never saves forced Off");
                }
            }, ref passed, ref failed);

            RunOne(output, "Display-on read failure retries without adopting a later firmware default", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Policy.SuppressForDisplayOff();
                f.Device.ReadFailures = 1;
                IoFailure(delegate { f.Policy.Poll(false, true, true); });
                f.Device.Level = 1;
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "display restore survives read failure");
                Equal(0, f.Saves.Count, "firmware default never saved");
            }, ref passed, ref failed);

            RunOne(output, "Display-on restore retries failures before and after the hardware write", delegate
            {
                for (int after = 0; after <= 1; after++)
                {
                    Fixture f = new Fixture(2, true);
                    f.Policy.SuppressForDisplayOff();
                    f.Device.WriteFailuresBefore = after == 0 ? 1 : 0;
                    f.Device.WriteFailuresAfter = after == 1 ? 1 : 0;
                    IoFailure(delegate { f.Policy.Poll(false, true, true); });
                    True(f.Policy.IsSuppressed, "failed restoration retains ownership");
                    f.Device.Level = 1;
                    f.Policy.Poll(false, true, false);
                    Equal(2, f.Device.Level, "retry restores remembered level");
                    False(f.Policy.IsSuppressed, "successful retry releases ownership");
                    Equal(0, f.Saves.Count, "retry never saves a firmware default");
                }
            }, ref passed, ref failed);

            RunOne(output, "A new display-off transition during a failed restoration preserves the snapshot", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Policy.SuppressForDisplayOff();
                f.Device.WriteFailuresBefore = 1;
                IoFailure(delegate { f.Policy.Poll(false, true, true); });
                f.Policy.SuppressForDisplayOff();
                f.Policy.Poll(false, true, true);
                Equal(2, f.Device.Level, "rapid off-on retains original brightness");
                Equal(0, f.Saves.Count, "rapid transitions cause no preference churn");
            }, ref passed, ref failed);

            RunOne(output, "Disabling automation and shutting down while display is off never relight it", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Policy.Poll(true, true, false);
                f.Policy.SuppressForDisplayOff();
                int reads = f.Device.Reads;
                int writes = f.Device.WriteAttempts;
                f.Policy.SetAutoEnabled(false);
                f.Policy.Shutdown();
                Equal(reads, f.Device.Reads, "off pause and exit perform no reads");
                Equal(writes, f.Device.WriteAttempts, "off pause and exit perform no writes");
                Equal(0, f.Device.Level, "display-off remains dark");
                f.Policy.Poll(true, true, true);
                Equal(2, f.Device.Level, "later active wake restores disabled automation preference");
            }, ref passed, ref failed);

            RunOne(output, "Tray choice while display is off changes only the desired wake level", delegate
            {
                for (int chosen = 0; chosen <= 2; chosen++)
                {
                    Fixture f = new Fixture(2, true);
                    f.Policy.Poll(true, true, false);
                    f.Policy.SuppressForDisplayOff();
                    int reads = f.Device.Reads;
                    int writes = f.Device.WriteAttempts;
                    f.Policy.ChooseLevel(chosen);
                    Equal(reads, f.Device.Reads, "off tray choice performs no reads");
                    Equal(writes, f.Device.WriteAttempts, "off tray choice performs no writes");
                    Equal(0, f.Device.Level, "off tray choice does not relight");
                    Equal(chosen, f.Policy.PreferredLevel, "desired level recorded");
                    f.Policy.Poll(true, true, true);
                    Equal(chosen, f.Device.Level, "explicit tray choice restored on wake");
                    True(f.Policy.ManualOverride, "tray override preserved in same fullscreen session");
                    f.Policy.Poll(false, true, false);
                    Equal(chosen, f.Device.Level, "no stale snapshot after explicit choice");
                }
            }, ref passed, ref failed);

            RunOne(output, "Hardware changes while the display is off cannot defeat display suppression", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Policy.SuppressForDisplayOff();
                f.Device.Level = 1;
                f.Policy.SuppressForDisplayOff();
                Equal(0, f.Device.Level, "screen-off suppression reapplied");
                f.Policy.Poll(false, true, true);
                Equal(2, f.Device.Level, "original preference survives hidden hardware change");
                Equal(0, f.Saves.Count, "hidden change is not saved as a manual preference");
            }, ref passed, ref failed);

            RunOne(output, "Turning automation on while display is off still respects fullscreen on wake", delegate
            {
                Fixture f = new Fixture(1, false);
                f.Policy.SuppressForDisplayOff();
                int writes = f.Device.WriteAttempts;
                f.Policy.SetAutoEnabled(true);
                Equal(writes, f.Device.WriteAttempts, "enabling automation does not relight");
                f.Policy.Poll(true, true, true);
                Equal(0, f.Device.Level, "enabled fullscreen automation keeps wake dark");
                f.Policy.Poll(false, true, false);
                Equal(1, f.Device.Level, "fullscreen exit restores initial preference");
            }, ref passed, ref failed);

            RunOne(output, "Shutdown after failed active display restoration can retry its remembered level", delegate
            {
                Fixture f = new Fixture(2, true);
                f.Policy.SuppressForDisplayOff();
                f.Device.WriteFailuresBefore = 1;
                IoFailure(delegate { f.Policy.Poll(false, true, true); });
                f.Device.Level = 1;
                f.Policy.Shutdown();
                Equal(2, f.Device.Level, "active exit completes pending display restore");
                Equal(0, f.Saves.Count, "active exit does not save firmware default");
            }, ref passed, ref failed);

            output.WriteLine("Display-off tests: {0} passed, {1} failed.", passed, failed);
            return failed == 0 ? 0 : 1;
        }

        private static void RunOne(TextWriter output, string name, Action test, ref int passed, ref int failed)
        {
            try { test(); passed++; output.WriteLine("PASS: " + name); }
            catch (Exception error) { failed++; output.WriteLine("FAIL: " + name + " -- " + error.Message); }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Object.Equals(expected, actual))
                throw new Exception(message + ": expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void False(bool condition, string message) { True(!condition, message); }

        private static void Sequence(int[] expected, List<int> actual, string message)
        {
            Equal(expected.Length, actual.Count, message + " count");
            for (int i = 0; i < expected.Length; i++) Equal(expected[i], actual[i], message + " item " + i);
        }

        private static void IoFailure(Action action)
        {
            bool threw = false;
            try { action(); } catch (IOException) { threw = true; }
            True(threw, "expected a simulated I/O failure");
        }

        private sealed class Fixture
        {
            public readonly FakeDevice Device;
            public readonly List<int> Saves = new List<int>();
            public readonly BacklightPolicy Policy;
            public Fixture(int level, bool autoEnabled)
            {
                Device = new FakeDevice(level);
                Policy = new BacklightPolicy(Device, level, autoEnabled, delegate(int chosen) { Saves.Add(chosen); });
            }
        }

        private sealed class FakeDevice : IBacklightDevice
        {
            public int Level;
            public int Reads;
            public int WriteAttempts;
            public int ReadFailures;
            public int WriteFailuresBefore;
            public int WriteFailuresAfter;
            public readonly List<int> Writes = new List<int>();
            public FakeDevice(int level) { Level = level; }
            public int ReadLevel()
            {
                Reads++;
                if (ReadFailures > 0) { ReadFailures--; throw new IOException("Simulated read failure."); }
                return Level;
            }
            public void WriteLevel(int level)
            {
                WriteAttempts++;
                if (WriteFailuresBefore > 0) { WriteFailuresBefore--; throw new IOException("Simulated write failure before hardware change."); }
                Level = level;
                Writes.Add(level);
                if (WriteFailuresAfter > 0) { WriteFailuresAfter--; throw new IOException("Simulated write failure after hardware change."); }
            }
        }
    }
}
