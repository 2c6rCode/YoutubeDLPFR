<div align="center">
  <img src="Shamiko_anime.webp" width="160" alt="YoutubeMaxxer Logo" style="border-radius: 12px;" />
  <h1>YoutubeMaxxer</h1>
  <p><b>A precision studio desktop frontend for yt-dlp on Windows.</b></p>
  <p>
    <a href="https://github.com/2c6rCode/YoutubeDLPFR/releases/latest"><img src="https://img.shields.io/github/v/release/2c6rCode/YoutubeDLPFR?style=flat-square&color=E11D48&label=Release" alt="Latest Release" /></a>
    <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-21262D?style=flat-square" alt="Platform" />
    <img src="https://img.shields.io/badge/Framework-.NET%2010%20WPF-21262D?style=flat-square" alt="Framework" />
    <img src="https://img.shields.io/badge/Engine-yt--dlp-21262D?style=flat-square" alt="Engine" />
  </p>
</div>

---

## Overview

**YoutubeMaxxer** is a modern, standalone Windows desktop interface for [yt-dlp](https://github.com/yt-dlp/yt-dlp). It brings the full depth of yt-dlp into a tactile studio dashboard—eliminating the need to memorize CLI flags, format selectors, and manual cookie exports.

Built with C# and .NET 10 WPF, featuring an obsidian dark theme, native Windows 11 immersive chrome, and real-time tabular telemetry.

---

## Key Features

### ✂️ Partial Video Cutting (`--download-sections`)
- Download only the specific time range you need (e.g. `01:30` to `03:45`) without fetching the entire video first.
- Saves bandwidth and disk space by requesting only matching byte ranges from streaming servers.
- Automatic `--force-keyframes-at-cuts` ensures clean cuts without frozen or black intro frames.

### 📋 Batch & Text File URL Ingestion
- **Drag-and-Drop:** Drop any `.txt`, `.csv`, or `.urls` file directly onto the window to instantly load all URLs.
- **Import Button:** Browse and select text files containing video, playlist, or channel links.
- **Batch Detector:** Displays a badge count (e.g. `14 URLs`) and queues all items sequentially in one click.
- Automatic comment stripping (ignores lines starting with `#`).

### 🍪 1-Click Browser Cookie Sessions (`--cookies-from-browser`)
- Direct session extraction from installed browsers: **Chrome**, **Edge**, **Firefox**, **Brave**, **Opera**, and **Vivaldi**.
- Bypasses YouTube bot verification, sign-in walls, age gates, and private playlist restrictions without manual cookie exporting.

### 🛡️ SponsorBlock Integration (`--sponsorblock-remove all`)
- Automatically queries the SponsorBlock community database to excise sponsored segments, intro animations, and self-promotion from downloaded media.

### 🌐 Network, Proxies & Geo-Restrictions
- Full support for HTTP, HTTPS, and SOCKS5 proxies (including Tor via `socks5://127.0.0.1:9050`).
- Geo-bypass spoofing with optional two-letter ISO country codes.

### ⚡ Engine & Media Craft
- **Tactile Mode Switcher:** Instant toggle between Video and Audio extraction modes.
- **Presets & Inspection:** Quick inspection panel for video metadata, uploader details, duration, and resolution tags.
- **Subtitles & Metadata:** Auto-embed subtitles, chapters, artist tags, and cover art directly into containers (`mp4`, `mkv`, `mp3`, `opus`, `flac`).
- **Telemetry:** Real-time transfer speed, downloaded bytes, ETA, and progress on monospaced tabular tracks.

---

## Installation & Quick Start

1. Download the latest standalone **`YtDlpGui.exe`** from the [Releases page](https://github.com/2c6rCode/YoutubeDLPFR/releases/latest).
2. Ensure you have **yt-dlp** and **FFmpeg** available on your machine (either in your system `PATH` or placed in the same folder as `YtDlpGui.exe`).
   - You can also configure custom executable paths directly in the in-app **Settings** tab.
3. Run `YtDlpGui.exe` and paste a URL or drop a `.txt` list to start downloading.

*(No .NET runtime installation required—the release binary is fully self-contained).*

---

## Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows 10 (Build 19041+) or Windows 11

### Build Instructions
```powershell
# Clone the repository
git clone https://github.com/2c6rCode/YoutubeDLPFR.git
cd YoutubeDLPFR

# Build debug binary
dotnet build

# Publish standalone single-file release executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

---

## License

This project is licensed under the [MIT License](LICENSE).
`yt-dlp` and `FFmpeg` are independent projects governed by their respective licenses.
