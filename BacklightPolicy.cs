using System;

namespace L14KeyboardLight
{
    public interface IBacklightDevice
    {
        int ReadLevel();
        void WriteLevel(int level);
    }

    // Call all methods from the same thread. Device failures propagate so callers
    // can report them; a later call retries without replacing a restore snapshot.
    public sealed class BacklightPolicy
    {
        private readonly IBacklightDevice device;
        private readonly Action<int> savePreferred;
        private bool hasRestoreSnapshot;
        private int restoreLevel;
        private bool pendingOffWrite;
        private bool resumePending;
        private bool preferenceSavePending;

        public BacklightPolicy(IBacklightDevice device, int preferredLevel,
            bool autoEnabled, Action<int> savePreferred)
        {
            if (device == null)
                throw new ArgumentNullException("device");
            ValidateLevel(preferredLevel);
            this.device = device;
            this.savePreferred = savePreferred;
            PreferredLevel = preferredLevel;
            LastObservedLevel = -1;
            AutoEnabled = autoEnabled;
        }

        public int PreferredLevel { get; private set; }
        public int LastObservedLevel { get; private set; }
        public bool AutoEnabled { get; private set; }
        public bool IsSuppressed { get; private set; }
        public bool IsFullscreen { get; private set; }
        public bool ManualOverride { get; private set; }

        public void Poll(bool fullscreen, bool active, bool resuming)
        {
            if (!active)
            {
                // Firmware can turn the light off while asleep or locked. Do
                // not sample that as a user's new preferred level.
                resumePending = true;
                return;
            }

            if (resuming)
                resumePending = true;
            IsFullscreen = fullscreen;
            if (!fullscreen)
                ManualOverride = false;

            int current = ReadCurrent();
            bool shouldSuppress = AutoEnabled && fullscreen && !ManualOverride;

            if (hasRestoreSnapshot)
            {
                if (pendingOffWrite && shouldSuppress)
                {
                    // A failed write may have taken effect in the device. Keep
                    // the snapshot until a later read resolves that uncertainty.
                    if (current != 0 && current != restoreLevel && !resumePending)
                        AcceptManualChange(current, fullscreen);
                    else
                        CompleteSuppression(current);
                    resumePending = false;
                    return;
                }

                if (resumePending && shouldSuppress)
                {
                    // A firmware reset on wake is not a manual override.
                    CompleteSuppression(current);
                    resumePending = false;
                    return;
                }

                if (current != 0)
                {
                    AcceptManualChange(current, fullscreen);
                    resumePending = false;
                    return;
                }

                if (shouldSuppress)
                {
                    resumePending = false;
                    return;
                }

                RestoreSnapshot(current);
                resumePending = false;
                return;
            }

            if (resumePending)
            {
                if (shouldSuppress)
                    BeginSuppression(PreferredLevel, current);
                else if (current == 0 && PreferredLevel != 0)
                    WriteCurrent(PreferredLevel);
                else
                    RememberPreference(current);
                resumePending = false;
                return;
            }

            if (shouldSuppress)
            {
                RememberPreference(current);
                BeginSuppression(current, current);
            }
            else
            {
                RememberPreference(current);
            }
        }

        public void ChooseLevel(int level)
        {
            ValidateLevel(level);
            // Do not release restoration ownership until the explicit choice
            // has been applied successfully.
            WriteCurrent(level);
            ClearSnapshot();
            ManualOverride = AutoEnabled && IsFullscreen;
            resumePending = false;
            RememberPreference(level);
        }

        public void SetAutoEnabled(bool enabled)
        {
            if (enabled && !AutoEnabled)
                ManualOverride = false;
            AutoEnabled = enabled;
            if (!enabled && hasRestoreSnapshot)
                RestoreSnapshot(ReadCurrent());
        }

        public void Shutdown()
        {
            AutoEnabled = false;
            if (hasRestoreSnapshot)
                RestoreSnapshot(ReadCurrent());
        }

        private void BeginSuppression(int snapshot, int current)
        {
            restoreLevel = snapshot;
            hasRestoreSnapshot = true;
            pendingOffWrite = true;
            CompleteSuppression(current);
        }

        private void CompleteSuppression(int current)
        {
            if (current != 0)
                WriteCurrent(0);
            pendingOffWrite = false;
            IsSuppressed = true;
        }

        private void AcceptManualChange(int current, bool fullscreen)
        {
            ClearSnapshot();
            ManualOverride = AutoEnabled && fullscreen;
            RememberPreference(current);
        }

        private void RestoreSnapshot(int current)
        {
            if (current != 0)
            {
                // Someone else changed the light; that choice takes precedence.
                ClearSnapshot();
                RememberPreference(current);
                return;
            }
            if (restoreLevel != 0)
                WriteCurrent(restoreLevel);
            ClearSnapshot();
        }

        private void ClearSnapshot()
        {
            hasRestoreSnapshot = false;
            pendingOffWrite = false;
            IsSuppressed = false;
        }

        private int ReadCurrent()
        {
            int level = device.ReadLevel();
            if (level < 0 || level > 2)
                throw new InvalidOperationException("The device returned an invalid backlight level.");
            LastObservedLevel = level;
            return level;
        }

        private void WriteCurrent(int level)
        {
            device.WriteLevel(level);
            LastObservedLevel = level;
        }

        private void RememberPreference(int level)
        {
            if (PreferredLevel != level)
            {
                PreferredLevel = level;
                preferenceSavePending = true;
            }
            if (preferenceSavePending)
            {
                if (savePreferred != null)
                    savePreferred(PreferredLevel);
                preferenceSavePending = false;
            }
        }

        private static void ValidateLevel(int level)
        {
            if (level < 0 || level > 2)
                throw new ArgumentOutOfRangeException("level", "Backlight level must be 0, 1, or 2.");
        }
    }
}
