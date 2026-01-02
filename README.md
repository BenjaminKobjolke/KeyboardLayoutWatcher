# Keyboard Layout Watcher

**Homepage & Download:** [https://workflow-tools.com/keyboard-layout-watcher](https://workflow-tools.com/keyboard-layout-watcher)

A Windows application that monitors and displays the current keyboard layout. Shows an alert when the keyboard layout changes.

## Screenshots

![Main Window](media/screenshot_01.jpg)

![Layout Changed Alert](media/screenshot_02.jpg)

## Features

- Detects keyboard layout changes in real-time
- Works even when the application is not focused
- Shows a popup alert when the layout changes (optional)
- Dark themed UI
- Supports common keyboard layouts (US, German, UK, French, Spanish, and many more)
- **Win+Space blocking** - Prevents accidental layout switches, requires triple-press to switch
- **System tray support** - Minimize to tray, restore with double-click or context menu
- **Persistent settings** - All preferences saved and restored on next launch

## Settings

- **Block Win+Space (triple-press to switch)** - Blocks the Win+Space shortcut to prevent accidental layout changes. Press Win+Space three times quickly to allow the switch.
- **Show alert on layout change** - Toggle the popup notification when keyboard layout changes
- **Minimize on start** - Start the application minimized
- **Minimize to tray** - When minimizing, hide to system tray instead of taskbar

## Requirements

- Windows
- .NET Framework 4.7.2

## Building

Run `build.bat` to compile the project. The executable will be created at:

```
KeyboardLayoutWatcher\bin\Release\KeyboardLayoutWatcher.exe
```

Alternatively, open `KeyboardLayoutWatcher.sln` in Visual Studio and build from there.

## Usage

1. Run `KeyboardLayoutWatcher.exe`
2. The main window displays the current keyboard layout
3. Configure settings using the checkboxes
4. When you change the keyboard layout (via Windows system tray or keyboard shortcut), a popup alert appears (if enabled)
5. Close the alert with Enter, Escape, or click OK
6. Minimize to tray for unobtrusive monitoring

## Supported Layouts

- US, UK, German, French, Italian, Spanish
- Portuguese (Brazil/Portugal), Canadian French
- Dutch, Swedish, Norwegian, Finnish, Danish
- Polish, Czech, Slovak, Hungarian, Romanian, Bulgarian
- Russian, Ukrainian, Greek, Turkish
- Hebrew, Arabic
- Japanese, Korean, Chinese (Traditional/Simplified)

Unknown layouts will display the raw keyboard layout ID.
