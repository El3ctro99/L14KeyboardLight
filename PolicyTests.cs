using System;
using System.Collections.Generic;
using System.IO;

namespace L14KeyboardLight
{
    public static class PolicyTests
    {
        public static int Run(TextWriter output)
        {
            if (output == null)
                throw new ArgumentNullException("output");
            int passed = 0;
            int failed = 0;

            RunOne(output, "Initial levels round-trip without saving forced Off", delegate
            {
                for (int level = 0; level <= 2; level++)
                {
                    Fixture f = new Fixture(level);
                    f.Policy.Poll(true, true, false);
                    Equal(0, f.Device.Level, "fullscreen level");
                    Equal(level, f.Policy.PreferredLevel, "remembered level");
                    True(f.Policy.IsSuppressed, "suppression active");
                    f.Policy.Poll(true, true, false);
                    f.Policy.Poll(false, true, false);
                    Equal(level, f.Device.Level, "restored level");
                    Equal(0, f.Saves.Count, "automatic saves");
                    Equal(level == 0 ? 0 : 2, f.Device.Writes.Count, "transition writes");
                    False(f.Policy.IsSuppressed, "suppression released");
                }
            }, ref passed, ref failed);

            RunOne(output, "Hardware choice overrides only the current fullscreen session", delegate
            {
                Fixture f = new Fixture(2);
                f.Policy.Poll(true, true, false);
                f.Device.Level = 1;
                f.Policy.Poll(true, true, false);
                True(f.Policy.ManualOverride, "manual override");
                False(f.Policy.IsSuppressed, "ownership released");
                Equal(1, f.Policy.PreferredLevel, "manual preference");
                f.Policy.Poll(true, true, false);
                Equal(1, f.Device.Writes.Count, "no repeated enforcement");
                f.Policy.Poll(false, true, false);
                False(f.Policy.ManualOverride, "override ends on fullscreen exit");
                f.Policy.Poll(true, true, false);
                Equal(0, f.Device.Level, "next session suppresses again");
                f.Policy.Poll(false, true, false);
                Equal(1, f.Device.Level, "next session restores manual preference");
                Sequence(new int[] { 1 }, f.Saves, "manual saves");
            }, ref passed, ref failed);

            RunOne(output, "Hardware Off outside fullscreen is a real preference", delegate
            {
                Fixture f = new Fixture(2);
                f.Device.Level = 0;
                f.Policy.Poll(false, true, false);
                Equal(0, f.Policy.PreferredLevel, "explicit hardware Off");
                Sequence(new int[] { 0 }, f.Saves, "Off saved outside suppression");
            }, ref passed, ref failed);

            RunOne(output, "Tray choices including Off cancel stale restoration", delegate
            {
                Fixture f = new Fixture(2);
                f.Policy.Poll(true, true, false);
                f.Policy.ChooseLevel(1);
                True(f.Policy.ManualOverride, "tray override");
                False(f.Policy.IsSuppressed, "tray releases ownership");
                f.Policy.Poll(true, true, false);
                Equal(1, f.Device.Level, "tray choice remains visible");
                f.Policy.ChooseLevel(0);
                f.Policy.Poll(false, true, false);
                f.Policy.Shutdown();
                Equal(0, f.Device.Level, "explicit Off remains Off");
                Sequence(new int[] { 1, 0 }, f.Saves, "tray saves");
            }, ref passed, ref failed);

            RunOne(output, "Inactive polls make no calls and preserve the snapshot", delegate
            {
                Fixture f = new Fixture(2);
                f.Policy.Poll(true, true, false);
                int reads = f.Device.Reads;
                int writes = f.Device.WriteAttempts;
                f.Device.Level = 0;
                for (int i = 0; i < 3; i++)
                    f.Policy.Poll(false, false, true);
                Equal(reads, f.Device.Reads, "inactive reads");
                Equal(writes, f.Device.WriteAttempts, "inactive writes");
                Equal(0, f.Saves.Count, "inactive saves");
                True(f.Policy.IsSuppressed, "snapshot retained");
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "resume restores even without repeated resuming flag");
            }, ref passed, ref failed);

            RunOne(output, "Resume outside fullscreen restores preference after firmware Off", delegate
            {
                Fixture f = new Fixture(2);
                f.Policy.Poll(false, true, false);
                f.Device.Level = 0;
                f.Policy.Poll(false, true, true);
                Equal(2, f.Device.Level, "resume restore");
                Equal(2, f.Policy.PreferredLevel, "resume preference");
                Equal(0, f.Saves.Count, "firmware Off not saved");
            }, ref passed, ref failed);

            RunOne(output, "Resume into fullscreen keeps Off and restores the remembered level", delegate
            {
                Fixture f = new Fixture(2);
                f.Device.Level = 0;
                f.Policy.Poll(true, true, true);
                Equal(0, f.Device.Level, "resume fullscreen Off");
                True(f.Policy.IsSuppressed, "resume owns suppression");
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "remembered preference restored");
                Equal(0, f.Saves.Count, "no forced Off save");
            }, ref passed, ref failed);

            RunOne(output, "Firmware wake reset is not a manual fullscreen override", delegate
            {
                Fixture f = new Fixture(2);
                f.Policy.Poll(true, true, false);
                f.Policy.Poll(false, false, false);
                f.Device.Level = 1;
                f.Policy.Poll(true, true, true);
                Equal(0, f.Device.Level, "fullscreen Off reapplied after wake");
                False(f.Policy.ManualOverride, "reset not treated as manual");
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "original snapshot survives wake");
            }, ref passed, ref failed);

            RunOne(output, "An Off preference remains Off through resume", delegate
            {
                Fixture f = new Fixture(0);
                f.Policy.Poll(false, false, false);
                f.Policy.Poll(false, true, true);
                Equal(0, f.Device.Level, "Off preference");
                Equal(0, f.Device.WriteAttempts, "no redundant Off writes");
                Equal(0, f.Saves.Count, "no redundant preference save");
            }, ref passed, ref failed);

            RunOne(output, "Read failure does not lose a pending resume", delegate
            {
                Fixture f = new Fixture(2);
                f.Device.Level = 0;
                f.Device.ReadFailures = 1;
                IoFailure(delegate { f.Policy.Poll(false, true, true); });
                Equal(-1, f.Policy.LastObservedLevel, "failed read is not observed");
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "resume retried");
                Equal(0, f.Saves.Count, "failed read did not save Off");
            }, ref passed, ref failed);

            RunOne(output, "Invalid device readings are rejected without writes or saves", delegate
            {
                Fixture f = new Fixture(2);
                f.Device.Level = 9;
                bool threw = false;
                try { f.Policy.Poll(true, true, false); }
                catch (InvalidOperationException) { threw = true; }
                True(threw, "invalid reading exception");
                Equal(2, f.Policy.PreferredLevel, "valid preference retained");
                Equal(0, f.Device.WriteAttempts, "invalid reading not acted on");
                Equal(0, f.Saves.Count, "invalid reading not saved");
            }, ref passed, ref failed);

            RunOne(output, "Failed suppression retries without capturing forced Off", delegate
            {
                Fixture f = new Fixture(2);
                f.Device.WriteFailuresBefore = 1;
                IoFailure(delegate { f.Policy.Poll(true, true, false); });
                False(f.Policy.IsSuppressed, "failed write not claimed successful");
                f.Device.ReadFailures = 1;
                IoFailure(delegate { f.Policy.Poll(true, true, false); });
                f.Policy.Poll(true, true, false);
                True(f.Policy.IsSuppressed, "suppression retried");
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "retry retains restore snapshot");
                Equal(0, f.Saves.Count, "no automatic preference save");
            }, ref passed, ref failed);

            RunOne(output, "A write that changes hardware then fails retains restoration", delegate
            {
                Fixture f = new Fixture(2);
                f.Device.WriteFailuresAfter = 1;
                IoFailure(delegate { f.Policy.Poll(true, true, false); });
                Equal(0, f.Device.Level, "failed call may already have applied Off");
                f.Policy.Poll(true, true, false);
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "uncertain write restored safely");
                Equal(2, f.Policy.PreferredLevel, "forced Off not captured");
                Equal(0, f.Saves.Count, "forced Off not saved");
            }, ref passed, ref failed);

            RunOne(output, "Restoration failure survives rapid fullscreen re-entry", delegate
            {
                Fixture f = new Fixture(2);
                f.Policy.Poll(true, true, false);
                f.Device.WriteFailuresBefore = 1;
                IoFailure(delegate { f.Policy.Poll(false, true, false); });
                True(f.Policy.IsSuppressed, "restoration ownership retained");
                f.Policy.Poll(true, true, false);
                f.Policy.Poll(false, true, false);
                Equal(2, f.Device.Level, "original level restored after re-entry");
                Equal(0, f.Saves.Count, "pending Off never becomes preference");
            }, ref passed, ref failed);

            RunOne(output, "Pause retries restoration and resumes automation when enabled", delegate
            {
                Fixture f = new Fixture(1);
                f.Policy.Poll(true, true, false);
                f.Device.WriteFailuresBefore = 1;
                IoFailure(delegate { f.Policy.SetAutoEnabled(false); });
                False(f.Policy.AutoEnabled, "pause takes effect despite failure");
                True(f.Policy.IsSuppressed, "pending restore retained");
                f.Policy.Poll(true, true, false);
                Equal(1, f.Device.Level, "paused restore retried by poll");
                f.Policy.SetAutoEnabled(true);
                f.Policy.Poll(true, true, false);
                Equal(0, f.Device.Level, "re-enabled automation");
                f.Policy.Shutdown();
                Equal(1, f.Device.Level, "shutdown restores");
            }, ref passed, ref failed);

            RunOne(output, "Pause and exit never overwrite a manual hardware change", delegate
            {
                Fixture paused = new Fixture(2);
                paused.Policy.Poll(true, true, false);
                paused.Device.Level = 1;
                paused.Policy.SetAutoEnabled(false);
                Equal(1, paused.Device.Level, "pause respects manual state");
                Equal(1, paused.Policy.PreferredLevel, "pause records manual choice");
                Equal(1, paused.Device.WriteAttempts, "pause does not restore stale level");

                Fixture exited = new Fixture(2);
                exited.Policy.Poll(true, true, false);
                exited.Device.Level = 1;
                exited.Policy.Shutdown();
                Equal(1, exited.Device.Level, "exit respects manual state");
                Equal(1, exited.Policy.PreferredLevel, "exit records manual choice");
                exited.Policy.Shutdown();
                Equal(1, exited.Device.WriteAttempts, "repeated exit has no stale ownership");
            }, ref passed, ref failed);

            RunOne(output, "Exit restoration can be retried after read and write failures", delegate
            {
                Fixture f = new Fixture(2);
                f.Policy.Poll(true, true, false);
                f.Device.ReadFailures = 1;
                IoFailure(delegate { f.Policy.Shutdown(); });
                f.Device.WriteFailuresBefore = 1;
                IoFailure(delegate { f.Policy.Shutdown(); });
                True(f.Policy.IsSuppressed, "exit failure keeps snapshot");
                f.Policy.Shutdown();
                Equal(2, f.Device.Level, "exit retry restores");
                False(f.Policy.IsSuppressed, "exit releases ownership");
                False(f.Policy.AutoEnabled, "exit disables automation");
            }, ref passed, ref failed);

            RunOne(output, "Failed tray choice retains the previous restoration snapshot", delegate
            {
                Fixture f = new Fixture(2);
                f.Policy.Poll(true, true, false);
                f.Device.WriteFailuresBefore = 1;
                IoFailure(delegate { f.Policy.ChooseLevel(1); });
                True(f.Policy.IsSuppressed, "failed choice retains ownership");
                False(f.Policy.ManualOverride, "failed choice not treated as applied");
                Equal(2, f.Policy.PreferredLevel, "failed choice not saved");
                f.Policy.Shutdown();
                Equal(2, f.Device.Level, "shutdown can still restore");
            }, ref passed, ref failed);

            RunOne(output, "Rapid successful fullscreen switches preserve the preference", delegate
            {
                Fixture f = new Fixture(2);
                for (int i = 0; i < 5; i++)
                {
                    f.Policy.Poll(true, true, false);
                    f.Policy.Poll(false, true, false);
                }
                Equal(2, f.Device.Level, "final restored level");
                Equal(2, f.Policy.PreferredLevel, "stable preference");
                Equal(10, f.Device.Writes.Count, "one write per transition");
                Equal(0, f.Saves.Count, "no preference churn");
            }, ref passed, ref failed);

            RunOne(output, "Preference save failure retries without touching hardware", delegate
            {
                FakeDevice device = new FakeDevice(1);
                int saves = 0;
                BacklightPolicy policy = new BacklightPolicy(device, 2, true, delegate(int level)
                {
                    saves++;
                    if (saves == 1)
                        throw new IOException("Simulated settings failure.");
                    Equal(1, level, "retry saves the intended preference");
                });
                IoFailure(delegate { policy.Poll(false, true, false); });
                policy.Poll(false, false, false);
                Equal(1, saves, "inactive does not retry saves");
                policy.Poll(false, true, false);
                Equal(2, saves, "save retried");
                Equal(0, device.WriteAttempts, "saving preference needs no hardware change");
            }, ref passed, ref failed);

            output.WriteLine("Policy tests: {0} passed, {1} failed.", passed, failed);
            return failed == 0 ? 0 : 1;
        }

        private static void RunOne(TextWriter output, string name, Action test,
            ref int passed, ref int failed)
        {
            try
            {
                test();
                passed++;
                output.WriteLine("PASS: " + name);
            }
            catch (Exception error)
            {
                failed++;
                output.WriteLine("FAIL: " + name + " -- " + error.Message);
            }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Object.Equals(expected, actual))
                throw new Exception(message + ": expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        private static void False(bool condition, string message)
        {
            True(!condition, message);
        }

        private static void Sequence(int[] expected, List<int> actual, string message)
        {
            Equal(expected.Length, actual.Count, message + " count");
            for (int i = 0; i < expected.Length; i++)
                Equal(expected[i], actual[i], message + " item " + i);
        }

        private static void IoFailure(Action action)
        {
            bool threw = false;
            try { action(); }
            catch (IOException) { threw = true; }
            True(threw, "expected a simulated I/O failure");
        }

        private sealed class Fixture
        {
            public readonly FakeDevice Device;
            public readonly List<int> Saves;
            public readonly BacklightPolicy Policy;

            public Fixture(int level)
            {
                Device = new FakeDevice(level);
                Saves = new List<int>();
                Policy = new BacklightPolicy(Device, level, true, delegate(int chosen)
                {
                    Saves.Add(chosen);
                });
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

            public FakeDevice(int level)
            {
                Level = level;
            }

            public int ReadLevel()
            {
                Reads++;
                if (ReadFailures > 0)
                {
                    ReadFailures--;
                    throw new IOException("Simulated read failure.");
                }
                return Level;
            }

            public void WriteLevel(int level)
            {
                WriteAttempts++;
                if (WriteFailuresBefore > 0)
                {
                    WriteFailuresBefore--;
                    throw new IOException("Simulated write failure before hardware change.");
                }
                Level = level;
                Writes.Add(level);
                if (WriteFailuresAfter > 0)
                {
                    WriteFailuresAfter--;
                    throw new IOException("Simulated write failure after hardware change.");
                }
            }
        }
    }
}
