# Matrix Screensaver

Digital rain in the style of The Matrix as a Windows screensaver, written in WPF on .NET 10.
Based on WBS Screensaver by Wm. Barrett Simms.

It began as a hobby project, slept through a long hibernation phase, and was finally finished
to my satisfaction with heavy use of Claude and Mistral.

## Features

- Runs on all monitors at once, each window fitted exactly to its own monitor and DPI scaling.
- Live preview in the Screen Saver Settings dialog; the settings window scrolls when it does not fit the screen.
- Character sets: Latin, Katakana, Hiragana, digits, symbols, Greek and Cyrillic, in any combination.
- Character size from 8 to 64 pixels, drop density from 1 to 20 new drops per column and minute.
- Colors: one fixed color with a color picker, a cycle through the whole color wheel, or every drop in its own color, each with a live preview strip.
- Glow around the bright characters, adjustable in intensity, radius and starting brightness level.
- Advanced rain behavior: per-drop speed and trail length ranges, drops starting in the middle of the screen, rare flash drops racing down the whole screen, drops ending early, fading characters that stick to their color, and flicker drops that stay in place and may run down afterwards.
- Frame rate from 1 to 60 frames per second.
- Settings persist in `%APPDATA%\MatrixScreenSaver\settings.json`; a broken file just falls back to the defaults.

## Build

```
dotnet publish MatrixScreenSaver/MatrixScreenSaver.csproj -p:PublishProfile=SingleFile
```

This creates `MatrixScreenSaver/bin/publish/MatrixScreenSaver.scr`, a single self-contained file that
needs no installed .NET.

## Install

1. Copy `MatrixScreenSaver.scr` to a permanent folder, e.g. `%LOCALAPPDATA%\Programs\MatrixScreenSaver`.
   Windows runs it from there.
2. Right-click it and choose **Install** (on Windows 11 under *Show more options*). Windows selects it
   as the screensaver and opens the Screen Saver Settings with a preview and the **Settings** button.

Outside of `C:\Windows\System32` it only stays in the screensaver list while it is selected. To remove
it, select another screensaver and delete the file. The settings are stored in
`%APPDATA%\MatrixScreenSaver\settings.json`.

## Command line

| Argument | Action |
|---|---|
| `/s` | Run the screensaver on all monitors. Mouse movement, a click or a key ends it. |
| `/c`, none | Show the settings. |
| `/p <HWND>` | Draw the preview into the given window. |

## License

MIT, see [LICENSE](LICENSE). Based on WBS Screensaver by Wm. Barrett Simms.
