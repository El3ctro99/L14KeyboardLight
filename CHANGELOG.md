# Changelog

## 1.1.1 — 2026-10-17

- Fix the keyboard backlight remaining on when Windows reports the display off, including while the session is locked.
- Apply display-off suppression immediately and recheck approximately every two seconds while the app can run. A dimmed display still counts as on.
- Keep display-off suppression active independently of the fullscreen checkbox and fullscreen manual overrides; pausing or exiting while the display is off does not relight the keyboard.
- Restore the remembered brightness after the display and session become active and the resume delay has elapsed, while preserving fullscreen suppression and manual overrides where applicable.
- Preserve restoration state across firmware defaults and failed device reads or writes without saving temporary Off as the preferred brightness.
- Add 19 display-off policy tests and 16 power-notification tests, bringing the automated suite to 102 passing tests.

## 1.1.0 — 2026-10-17

- Add English and Hungarian tray menus, status dialogs, tooltips and app messages.
- Add a **Language / Nyelv → English / Magyar** submenu for immediate switching without changing keyboard brightness.
- Choose Hungarian on first run when the Windows display language is Hungarian, and English otherwise; remember an explicit language choice across launches.
- Load 1.0.0 settings without changing the existing brightness or automatic-fullscreen preference.
- Add localization regression tests alongside the existing 20 policy tests and 30 fullscreen-geometry tests.
- Update the English and Hungarian documentation with both sets of menu labels.

## 1.0.0 — 2026-10-17

Initial release for ThinkPad L14 Gen 3 Intel, machine type 21C2, on Windows 11 x64.

- Control Off, Low and High using the already installed Lenovo power-management driver.
- Turn off the keyboard backlight for foreground fullscreen windows and restore the previous level afterward.
- Respect manual brightness changes for the current fullscreen session.
- Provide Hungarian tray controls, per-user startup, saved preferences and rolling diagnostics.
- Handle session inactivity and resume notifications without saving firmware-induced Off as the preferred brightness.
- Include a Windows .NET Framework build script, portable executable support and per-user installation/removal scripts.
- Make migration from the known older Keyboard Backlight for Lenovo controller opt-in, with a persistent backup and restoration option.
- Include 20 policy tests and 30 fullscreen-geometry tests, all passing locally.

Hardware brightness control and visible fullscreen transitions were verified on one supported laptop. Actual sleep, lock and display-off cycles, exclusive fullscreen games and multiple physical monitors were not tested locally.
