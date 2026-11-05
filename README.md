# Localhost Manager

> A lightweight, minimalist Windows 11 desktop utility to monitor, browse, and terminate active localhost dev servers and ports with zero idle CPU and minimal RAM footprint.

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D4?style=flat-square&logo=windows)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)
[![Design](https://img.shields.io/badge/UI-Fluent%20Dark-1F1F1F?style=flat-square)](https://fluent2.microsoft.design)

---

## ⚡ Highlights

- **0% Idle CPU Usage**: No background polling loops, timers, or battery drain. Sockets are scanned purely on-demand via direct Win32 `iphlpapi.dll` P/Invoke (`GetExtendedTcpTable`).
- **Low Memory Footprint**: Runs in ~30–50 MB RAM with proactive working set trimming on minimization.
- **Windows 11 Fluent Dark Aesthetic**: Seamless DWM Immersive Dark Mode title bar, matte dark acrylic palette, vector iconography, and no emojis.
- **Deep Process Tree Killer**: Terminates stubborn parent and child processes (Node.js, Vite, Next.js, Python/Uvicorn, Java, Docker, PostgreSQL, MySQL) in a single click.
- **Smart Elevation Fallback**: Runs as standard user (`asInvoker`) to avoid annoying UAC prompts on every launch; dynamically escalates via `taskkill /F /T` only when terminating system or protected services.
- **Multi-Language Support**: Seamless runtime localization for English, Русский, Español, Deutsch, Français, and 中文.
- **System Tray Integration**: Minimize cleanly to the system notification tray with quick action context menus.

---

## 🛠️ Tech Stack

- **Platform**: .NET 8.0 (`net8.0-windows`)
- **UI Framework**: Windows Presentation Foundation (WPF) + XAML
- **System Integration**:
  - `iphlpapi.dll` P/Invoke for IPv4 & IPv6 TCP table listener detection
  - `dwmapi.dll` for Windows 11 Immersive Dark Titlebar (`DWMWA_USE_IMMERSIVE_DARK_MODE`)
  - `kernel32.dll` (`SetProcessWorkingSetSize`) for RAM optimization
  - `System.Windows.Forms.NotifyIcon` for System Tray docking

---

## 🚀 Getting Started

### Prerequisites
- Windows 10 (1809+) or Windows 11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (or SDK)

### Building from Source

```powershell
# Clone repository
git clone https://github.com/ShustovRoman/LocalHostManager.git
cd LocalHostManager

# Build and publish release binary
dotnet publish LocalhostManager.csproj -c Release -o ./Publish
```

The compiled standalone executable will be located at `./Publish/LocalhostManager.exe`.

---

## ⌨️ Shortcuts & Controls

| Action | Control / Shortcut |
|---|---|
| **Refresh Ports List** | `F5` or `Обновить` / `Refresh` Button |
| **Open Host in Browser** | `Enter`, double-click row, or click `Open` |
| **Terminate Process Tree** | `[ × ]` Button |
| **Context Menu** | Right-click any row (Copy URL, Copy Port, Copy PID, Open File Location) |
| **Minimize to Tray** | Window Close `[X]` button |
| **Restore from Tray** | Double-click tray icon or tray context menu |

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
