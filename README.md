<div align="center">
  <img src="Shamiko_anime.webp" width="140" alt="YoutubeMaxxer Logo" />
  <h1>YoutubeMaxxer</h1>
  <p>Desktop graphical interface for yt-dlp on Windows.</p>
  <p>
    <a href="https://github.com/2c6rCode/YoutubeDLPFR/releases/latest"><img src="https://img.shields.io/github/v/release/2c6rCode/YoutubeDLPFR?style=flat-square&color=21262D&label=release" alt="Latest Release" /></a>
    <img src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-21262D?style=flat-square" alt="Platform" />
    <img src="https://img.shields.io/badge/dotnet-10.0%20WPF-21262D?style=flat-square" alt="Framework" />
    <img src="https://img.shields.io/badge/engine-yt--dlp-21262D?style=flat-square" alt="Engine" />
  </p>
</div>

---

## Overview

YoutubeMaxxer is a native Windows desktop frontend for [yt-dlp](https://github.com/yt-dlp/yt-dlp). It exposes command-line arguments for extraction, clipping, authentication, and post-processing through a structured desktop interface built with C# and .NET 10.

---

## Features

### Time-Range Cutting
- Extracts specific duration intervals using `--download-sections` (e.g., `*01:30-03:45`).
- Streams only the necessary byte ranges from the host server.
- Uses `--force-keyframes-at-cuts` to prevent keyframe boundary artifacts.

### Batch and Text File Ingestion
- Accepts multiple URLs via drag-and-drop of `.txt`, `.csv`, or `.urls` files directly into the window.
- In-app file picker imports lists with automatic comment exclusion (lines beginning with `#`).
- Multi-line detection queues batch items sequentially in a single pass.

### Browser Session Cookies
- Direct session extraction via `--cookies-from-browser` for Chrome, Edge, Firefox, Brave, Opera, and Vivaldi.
- Bypasses bot gates, age restrictions, and access-limited playlists without manual `.txt` cookie exports.

### SponsorBlock
- Automatically queries the SponsorBlock API to excise sponsored segments, intros, and self-promotions using `--sponsorblock-remove all`.

### Network and Geo-Bypass
- Proxy support for HTTP, HTTPS, and SOCKS5 endpoints (including Tor routing via `socks5://127.0.0.1:9050`).
- Geo-restriction bypass options with two-letter ISO country code simulation.

### Container and Audio Formatting
- Separate Video and Audio extraction pipelines.
- Formats: MP4, MKV, WebM, MP3, M4A, Opus, WAV, FLAC.
- Embeds metadata tags, chapters, subtitles, and cover art thumbnails into container files.
- Real-time download speed, ETA, and byte counters using tabular monospace rendering.

---

## Installation

1. Download `YtDlpGui.exe` from the [Releases](https://github.com/2c6rCode/YoutubeDLPFR/releases/latest) page.
2. Ensure `yt-dlp` and `ffmpeg` are available in your system `PATH` or placed in the same folder as `YtDlpGui.exe`. Custom paths can also be assigned in the Settings tab.
3. Launch `YtDlpGui.exe`. The release binary is self-contained and does not require a local .NET installation.

---

## Building from Source

### Requirements
- .NET 10 SDK
- Windows 10 (Build 19041+) or Windows 11

### Commands
```powershell
# Clone repository
git clone https://github.com/2c6rCode/YoutubeDLPFR.git
cd YoutubeDLPFR

# Debug build
dotnet build

# Publish standalone single-file binary
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

---

## License

This project is licensed under the [MIT License](LICENSE).
yt-dlp and FFmpeg are distributed under their own respective licenses.
