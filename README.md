<div align="center">
  <img src="assets/icon.png" width="96" alt="IconFlow logo">
  <h1>IconFlow</h1>
  <p><strong>A lightweight, local-first icon manager built natively for Windows 11.</strong></p>
  <p>
    English · <a href="README.zh-CN.md">简体中文</a> ·
    <a href="README.ja.md">日本語</a> · <a href="README.es.md">Español</a>
  </p>
</div>

![IconFlow demo](docs/media/iconflow-demo.gif)

## Why IconFlow exists

Changing a Windows icon should be a small, safe act of personal organization—not a lesson in ICO layers, `desktop.ini`, shortcut internals, registry keys, or Explorer caches.

IconFlow turns that fragmented workflow into a reversible visual action. It keeps the original state, stores a stable copy of every applied icon, refreshes Explorer for you, and makes recovery a first-class feature. The goal is simple: give people a calmer, more legible workspace without taking control of their files or sending their data elsewhere.

## What makes it different

- **Native and fast.** .NET 8, Windows App SDK and WinUI 3; no Chromium process.
- **Right where you need it.** A compact, always-on-top picker opens from File Explorer and stays inside the screen edge.
- **Local by design.** No account, cloud sync, telemetry, continuous network connection, or file upload.
- **Reversible.** Every change is backed up and can be undone or restored to the Windows default.
- **Sharp at every size.** Imported PNG, JPG, WEBP, SVG, BMP and ICO files become seven-layer ICO files from 16 to 256 px.
- **Organized, not accumulated.** Search, favorites, folders, tags, duplicate detection and safe deletion keep the library usable.
- **International.** The app ships with 11 interface languages, including RTL support for Arabic.

## Highlights

- Change icons for folders and `.lnk` shortcuts.
- Native Windows 11 first-level context menu through `IExplorerCommand`.
- Current-user compatibility menu without administrator privileges.
- Drag an image into either the main library or compact picker and use it immediately.
- Crop, scale, add padding, rounded corners, a base shape and background removal.
- Preview from the largest embedded ICO layer to avoid blurry library thumbnails.
- Grouped history with before/after previews, open-location and undo actions.
- Move the icon library to another location with copy and SHA-256 verification.
- Light, dark and system themes with 100–200% DPI support.

## Supported languages

简体中文, 繁體中文, English, Español, Français, Deutsch, Português (Brasil), 日本語, 한국어, Русский and العربية.

## Download and run

Download the latest portable ZIP from **Releases**, extract it to a stable folder, then run `IconFlow.exe`.

Requirements:

- Windows 10 22H2 or Windows 11 23H2+
- x64
- .NET 8 Desktop Runtime
- Windows App Runtime 2.4

IconFlow does not start with Windows or stay in the tray by default. If silent startup is enabled later, it performs lightweight maintenance without opening a window and exits.

### Windows 11 context menu

For the native first-level menu, run `Install-Win11Menu.ps1` from the release folder or use **Settings → Install / Repair Windows 11 menu**. This optional step installs a signed sparse identity package and requires one UAC confirmation. The portable compatibility menu remains available without elevation under **Show more options**.

> The current community build uses a project-local development certificate for the optional sparse package. Review `SECURITY.md` and the release notes before installing it. The portable application itself does not require certificate installation.

## Privacy

Application data is stored under `%LocalAppData%\IconFlow`. IconFlow does not upload file names, paths, desktop contents, installed-app lists, imported images or operation history. See [PRIVACY.md](PRIVACY.md) for the complete data boundary.

## Build from source

```powershell
dotnet build native\IconFlow.WinUI\IconFlow.WinUI.csproj -c Release
dotnet run --project native\IconFlow.Native.Tests\IconFlow.Native.Tests.csproj -c Release
powershell -ExecutionPolicy Bypass -File scripts\build-native.ps1
```

Architecture:

- `native/IconFlow.Core` — storage, image conversion, history and Windows integration.
- `native/IconFlow.WinUI` — native Fluent interface.
- `native/IconFlow.ShellExtension` — Windows 11 `IExplorerCommand` extension.
- `native/IconFlow.Native.Tests` — executable regression suite.

## Current scope

The stable core covers folders and shortcuts. System desktop icons, file-type associations, taskbar icons, Start menu internals and bulk matching rules remain out of scope for this release.

## Contributing

Bug reports, translations and focused improvements are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) and [SECURITY.md](SECURITY.md) first.

## License

[MIT](LICENSE) © IconFlow contributors.
