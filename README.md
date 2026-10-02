# Matrix Screensaver

Digital rain in the style of The Matrix as a Windows screensaver, written in WPF on .NET 10.
Based on WBS Screensaver by Wm. Barrett Simms.

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
