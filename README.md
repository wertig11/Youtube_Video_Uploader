# YouTube Video Uploader (WPF Desktop App)

[![Download](https://img.shields.io/github/v/release/wertig11/Youtube_Video_Uploader?label=Download%20Installer&color=blue)](https://github.com/wertig11/Youtube_Video_Uploader/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A modern, robust, clean architecture C# .NET 8 desktop application that combines automated local video file renaming and batch scheduled uploading to YouTube with resumable upload tracking, custom interval scheduling, preset management, and multi-language support.

---

## Features

### 📁 1. Smart File Renaming
* **Pattern-Based Renaming**: Supports naming templates utilizing counter placeholders (e.g. `{Number}`) and level titles (e.g. `{LevelName}`).
* **Creation-Date Parsing**: Formats names using creation/recording dates, supporting regex formats (e.g., `{Date:yyyy-MM-dd}`).
* **Level Name Pasting**: Allows pasting names copied directly from a chatbot or document into a text area for mapping.
* **Undo Support**: Reverts the last executed batch of renames on disk if a mistake was made.
* **Name Conflict Protection**: Automatically generates unique names to prevent overwriting existing files in the folder.

### ⬆ 2. Batch YouTube Uploading
* **Custom Interval Scheduling**: Set a start date and an interval in days (e.g., public release every 2 days at a specific time) rather than daily only.
* **Resumable Chunked Uploads**: Direct integration with the `Google.Apis.YouTube.v3` SDK supporting resumable uploads.
* **Automated Playlist Assignment**: Adds uploaded videos to designated playlists automatically.
* **Tag Generation**: Trims and extracts dynamic tags automatically based on level/game names (deduplicated).
* **Safe Resume**: Tracks uploaded videos in a plain-text history log (`uploaded_log.txt`) to automatically skip files in case of cancellation or crash.

### ⚙ 3. UI, Configuration, & State Persistence
* **Theme Styling & Spacing**: A beautiful, spacious user interface designed with modern alignment and padding to avoid clipping.
* **Est. Time Remaining (ETA)**: Live speed (B/s, KB/s, MB/s) and ETA calculations for the current video and the overall queue.
* **Presets Management**: Create, save, import, and export profile configurations.
* **State Persistence**: Restores all input fields and prepared queues instantly after application restart or crashes.
* **Google OAuth Auto-Load**: Remembers and reuses your `client_secret.json` path once selected.
* **Multi-Language Switch**: Instantly toggles between English and Ukrainian without restarting.

---

## Clean Architecture Structure
Dependencies flow strictly inwards following SOLID, GRASP, and GoF patterns:
* **YouTubeVideoUploader.Domain**: Core models (`VideoFile`, `UploadJob`), Value Objects (`PublishSchedule`, `RenameTemplate`), and abstract interfaces.
* **YouTubeVideoUploader.Application**: Service orchestrators (`UploadOrchestrator`, `RenameService`), tag engines, and renaming strategies.
* **YouTubeVideoUploader.Infrastructure**: Implementation of adapters (Google API `YouTubeApiAdapter`), auth services, and JSON/TXT repositories.
* **YouTubeVideoUploader.UI**: WPF desktop layer utilizing MVVM CommunityToolkit, containing Views and ViewModels.

---

## Installation & Deployment

### Download the Installer
1. Go to the [Releases](https://github.com/wertig11/Youtube_Video_Uploader/releases) page on GitHub.
2. Download the latest release asset `YouTubeVideoUploader_v2.0.1_Setup.zip`.
3. Extract the archive contents to a folder on your computer.

### Run the Installation Script
You can easily set up the application and add it to your start menu or desktop:
1. Open PowerShell inside the extracted folder.
2. Run the installer:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install.ps1
   ```
3. Follow the prompt to choose whether to create a Desktop shortcut.

---

## Development & Building

### Prerequisites
* .NET 8.0 SDK or higher
* Visual Studio 2022 or JetBrains Rider

### Build Steps
Restore dependencies and compile the solution:
```bash
dotnet build
```

Run unit tests (covering rename strategies, tag generators, and schedule calculators):
```bash
dotnet test
```

Publish a self-contained, single-file executable package:
```bash
dotnet publish src/YouTubeVideoUploader.UI/YouTubeVideoUploader.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o Builds
```

---

## License
Licensed under the [MIT License](LICENSE).
