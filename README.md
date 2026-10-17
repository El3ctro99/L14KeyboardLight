# L14 Keyboard Light

A small Windows tray app that turns off a ThinkPad's keyboard backlight while the foreground window is fullscreen, then restores its previous brightness.

**Supported hardware: ThinkPad L14 Gen 3 Intel, machine type 21C2, with a backlit keyboard and Lenovo's existing power-management driver.** This release deliberately rejects other model types and unexpected driver responses. It has been tested on one 21C2 laptop running Windows 11.

[Magyar leírás](README.hu.md)

## Features

- Off, Low and High brightness from the tray menu.
- Automatic fullscreen suppression and restoration of the previous level.
- Manual overrides with **Fn+Space** or the tray menu. An override lasts for the current fullscreen session.
- Optional startup with the current Windows account.
- Handling for lock, display-off and resume notifications, with simulated tests for those state transitions.
- No network access, telemetry or automatic updater.

The app uses the **already installed Lenovo `IBMPmDrv` driver**. It does not install a kernel driver or include WinRing0, TVicPort or PawnIO. Lenovo Vantage and a separate .NET Desktop Runtime are not required. The app runs as a normal user.

The tray menu and runtime messages are currently in Hungarian.

## Requirements

- Windows 11 x64.
- ThinkPad L14 Gen 3 Intel, type **21C2**, with a backlit keyboard.
- A working Lenovo power-management driver exposing `\\.\IBMPmDrv`.
- Windows' .NET Framework 4.8 or later. Development and local testing used the built-in .NET Framework 4.8.1.

Do not run another keyboard-backlight controller alongside this app. Two controllers can overwrite each other's brightness changes.

## Download

Download the Windows x64 ZIP from [Releases](https://github.com/El3ctro99/L14KeyboardLight/releases), extract it, then run `bin\L14KeyboardLight.exe`. The keyboard icon appears near the clock. Installation is optional; see below for automatic startup and migration from the older utility.

## Build and run

The project uses Windows Forms and C# 5. The build script calls the .NET Framework compiler included with Windows; it downloads nothing and needs no .NET SDK or NuGet packages.

From the source directory in PowerShell 7:

```powershell
pwsh -File .\build.ps1
.\bin\L14KeyboardLight.exe
```

Running the executable directly is the **portable option**: no installation is necessary. Settings are still saved under your local application-data directory. If you enable startup in the tray menu, keep the executable at the same path.

Only run scripts obtained from a source you trust and follow your organization's PowerShell execution policy.

## Install or remove

For a per-user installation:

```powershell
pwsh -File .\install.ps1
```

The installer copies the app and removal scripts to `%LOCALAPPDATA%\Programs\L14KeyboardLight`, creates a Start menu shortcut, enables startup for your account and launches the app. A normal installation does not require administrator rights.

If the older **Keyboard Backlight for Lenovo** utility is installed, close it and disable its automatic startup and backlight-reset service before using this app. Alternatively, the following explicit migration option backs up and disables the known older controller's startup entries and reset service:

```powershell
pwsh -File .\install.ps1 -MigratePrevious
```

Migration requires Windows administrator approval because it changes the older utility's machine-wide settings. Its backup is retained at `%LOCALAPPDATA%\L14KeyboardLight\legacy-backup`. It does not remove the Lenovo hardware driver or shared runtimes.

Remove the installed app using the installed script:

```powershell
pwsh -File "$env:LOCALAPPDATA\Programs\L14KeyboardLight\remove.ps1"
```

To also restore a previously migrated controller, add `-RestorePrevious`. Restoration requires administrator approval. Settings and migration backups are retained after removal.

## Tray controls

Right-click the keyboard icon near the clock. It may be in the hidden-icons area.

| Hungarian menu item | Action |
| --- | --- |
| **Ki / Halvány / Erős** | Set and remember Off / Low / High. |
| **Teljes képernyőn automatikusan kikapcsol** | Enable or pause automatic fullscreen suppression. |
| **Indítás a Windowszal** | Enable or disable startup for your Windows account. |
| **Állapot…** | Show the model, current policy state and last error. Double-clicking the icon also opens this dialog. |
| **Kilépés** | Exit and restore brightness if this app still owns the automatic suppression. |

Turning off automation or exiting restores a light the app suppressed, without overwriting a manual brightness change. A new fullscreen session enables suppression again after a manual override.

## Fullscreen detection and limitations

Every approximately 350 ms, the app compares the foreground window with the full bounds of that window's monitor. It excludes the desktop, lock screen and ordinary maximized windows. The geometry logic accounts for multiple monitors, negative coordinates and DPI differences.

This is a **window-size heuristic**, not video-playback detection. A paused fullscreen video still counts as fullscreen. A borderless window manually sized to fill a monitor may also count. Exclusive fullscreen games and multiple physical monitors have not been tested locally.

While Windows reports the session inactive, the app avoids treating a firmware-induced Off state as a new brightness preference. On return it waits briefly, then reevaluates fullscreen state and restores the remembered level when appropriate. Actual locking, display-off and sleep cycles were not induced during local validation; their policy behavior is covered by simulated tests.

## Settings and diagnostics

Settings and rolling diagnostic logs are stored in `%LOCALAPPDATA%\L14KeyboardLight`. The log rotates at approximately 256 KB and retains one previous file.

The following commands do not start a second tray controller:

```powershell
# Run 20 policy tests and 30 fullscreen-geometry tests; no hardware writes.
Start-Process .\bin\L14KeyboardLight.exe -ArgumentList '--self-test', 'self-test.txt' -Wait
Get-Content .\self-test.txt

# Read model, hardware brightness and foreground-window diagnostics.
Start-Process .\bin\L14KeyboardLight.exe -ArgumentList '--status', 'status.txt' -Wait

# Request a graceful exit of the app in the current Windows session.
.\bin\L14KeyboardLight.exe --stop
```

When sharing a status report, review its machine and foreground-window details first.

## Validation

- All **50 simulated tests** passed: 20 policy tests and 30 geometry tests.
- On one L14 Gen 3 Intel 21C2, all three physical brightness levels were read and set successfully.
- A visible test window verified fullscreen Off, restoration on return to windowed mode, repeated entry, manual override, exclusion of ordinary maximization and restoration on clean exit.
- Actual sleep, lock and display-off cycles, exclusive fullscreen games and multiple physical monitors remain unverified locally.

See [VALIDATION.md](VALIDATION.md) for the original Hungarian validation record and [CHANGELOG.md](CHANGELOG.md) for changes.

## Driver protocol reference

The [SaltwaterC/KeyboardBacklightForLenovo](https://github.com/SaltwaterC/KeyboardBacklightForLenovo) project was used as a reference for Lenovo's driver interface. The L14-specific response values were separately read and round-trip tested on the supported laptop.

The app uses `IBMPmDrv` IOCTLs `0x00222680` (read) and `0x00222684` (write). This hardware returned `0x00250200`, `0x00250201` and `0x00250202` for Off, Low and High. Unsupported models and other read responses fail explicitly; there is no fallback to direct embedded-controller or physical-memory access.

## License

[MIT](LICENSE). Copyright (c) 2026 El3ctro99. Lenovo and ThinkPad are trademarks of their respective owners; this is an independent project.
