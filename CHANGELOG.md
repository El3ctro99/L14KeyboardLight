# Changelog

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
