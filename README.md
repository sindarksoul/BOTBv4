# 🚀 BestOfTheBest (BOTB) — v4 Classic Edition

<div align="center">

<img src="https://github.com/sindarksoul/BOTBv2/blob/main/appicon.ico" alt="BOTB application icon" width="90" />

## 🔥 BOTBv4 — The BestOfTheBest, Evolved 🔥

**Local text analysis • Smart filtering • 7-Zip extraction • Telegram Desktop integration**

**Built for Windows with C# and .NET 8 WPF**

[![Windows](https://img.shields.io/badge/Windows-Desktop-0078D4?logo=windows)](https://www.microsoft.com/windows)
[![.NET 8](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C%23](https://img.shields.io/badge/C%23-WPF-239120)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/License-MIT-brightgreen)](LICENSE)

**Developed by [@sindarksoul](https://github.com/sindarksoul)**

</div>

---

## 🚀 What Is BOTBv4?

**BestOfTheBest (BOTB)** is a Windows desktop application for reviewing and organizing **locally stored text records** with keyword filtering and duplicate removal. Version 4 expands the classic workflow with a dedicated **7-Zip archive extractor** and **Telegram Desktop integration**, wrapped in a sleek dark interface with cyan accents.

BOTB is designed for files you own or are explicitly authorized to review. It is not an online authentication tool, account checker, or network scanner.

## ✨ What's New in v4?

- 📦 **Integrated 7-Zip extraction:** Select an archive, choose a destination, and follow an animated progress display.
- 🔐 **Protected archive support:** Password prompt when needed for an encrypted archive.
- 🤖 **Robot-powered progress UI:** A playful 20-robot progress indicator and animated preparation spinner.
- ✈️ **Telegram Desktop window:** Launch Telegram from BOTB and switch between stable separate-window mode and experimental embedding.
- 🖥️ **Refined desktop styling:** Dark WPF panels, cyan highlights, readable status console, and branded app icon.
- 🧹 **Automatic result deduplication:** Repeated matches are filtered within each keyword's result set.
- 📂 **Convenient output access:** Open the current output folder directly from the app.

> **AI integration status:** The checked BOTBv4 project does not currently include a separate AI button, AI service, model runtime, or AI-specific class. Telegram integration is present, but an AI assistant inside Telegram/BOTB cannot be confirmed from this source snapshot. AI is therefore **not advertised as a working v4 feature** in this README.

## 🆚 BOTBv2 vs. BOTBv4

| Capability | BOTBv2 | BOTBv4 (checked source) |
| --- | --- | --- |
| Single-line text mode | ✅ | ✅ |
| Multi-line folder mode | ✅ | ✅ |
| Keyword-based filtering | ✅ | ✅ (up to 2 keywords) |
| Duplicate removal | ✅ | ✅ |
| Scrollable progress log | ✅ | ✅ |
| Built-in archive extraction window | — | ✅ 7-Zip |
| Password-protected archive prompt | — | ✅ |
| Animated archive progress | — | ✅ |
| Telegram Desktop launcher | — | ✅ |
| In-window Telegram embedding | — | 🧪 Experimental |

*The v2 column reflects the supplied BOTBv2 README. The v4 column reflects the current C#/XAML project files.*

## 🧩 Core Modules

### 📄 Local Text Processing

BOTB provides two choices for authorized local files:

**Single-line mode** works with a selected text file. **Multi-line mode** searches a selected folder and its subfolders for supported structured text files. Both modes offer case-insensitive keyword filtering, deduplicated results, and a scrolling status log. A maximum of two keywords can be supplied at once, separated by commas or semicolons.

Output files are written in the selected input's location and named after the corresponding keyword. Because those outputs may include private records, keep them securely stored and do not commit them to a public repository.

### 📦 Built-In 7-Zip Archive Extractor

The **Extract Archive (7-Zip)** button opens an independent archive window. It supports selecting an archive and a destination parent folder, checks the archive, prompts for a password when appropriate, and shows progress through animated robot icons. Extraction takes place in a temporary staging folder before the completed contents are moved to a final folder named after the archive.

The picker recognizes common archive extensions including `.zip`, `.7z`, `.rar`, `.tar`, `.gz`, `.bz2`, `.xz`, `.cab`, and `.iso`. Actual extraction compatibility follows the bundled 7-Zip engine. Existing destination folders are not overwritten by the archive workflow.

### ✈️ Telegram Desktop Integration

The **Open Telegram Desktop** button launches a companion Telegram window with these actions:

- **Open Separately:** Recommended, stable Telegram Desktop experience.
- **Try Embed (Experimental):** Attempts to display Telegram inside the BOTB window.
- **Detach:** Restores the Telegram window to its normal separate desktop view.

Telegram handles its own sign-in. **Telegram Desktop is not bundled** with BOTBv4.

⚠️ **Current limitation:** The executable path is hard-coded in `TelegramWindow.cs` to `C:\Users\Admin\AppData\Roaming\Telegram Desktop\Telegram.exe`. Other systems need this path adapted in the source. Cross-process embedding may freeze or fail; **Open Separately** is the fallback. Closing BOTB's Telegram window also attempts to close the matching Telegram Desktop process.

## 🌟 Getting Started

1. Install **Visual Studio 2022** with the **.NET desktop development** workload or install the **.NET 8 SDK**.
2. Clone this repository:

   ```powershell
   git clone https://github.com/sindarksoul/BOTBv4.git
   cd BOTBv4
   ```

3. Open `BOTB_PROJECT/BOTB/BOTB.sln` in Visual Studio and build the project, or run:

   ```powershell
   dotnet restore .\BOTB_PROJECT\BOTB\BOTB\BOTB.csproj
   dotnet build .\BOTB_PROJECT\BOTB\BOTB\BOTB.csproj -c Release
   ```

4. Launch the resulting `BOTB.exe`. Keep `Tools/7zip` alongside the application's output files so the archive extractor can find its engine.
5. To use Telegram, install Telegram Desktop and update the configured executable path if your installation differs.

> **Downloads:** See [GitHub Releases](https://github.com/sindarksoul/BOTBv4/releases) for published builds, if available. A self-contained installer or a single-file distribution is **not guaranteed** by the current project configuration.

## 🛠️ Project Structure

```text
BOTBv4/
├── .gitignore
├── LICENSE
├── README.md
└── BOTB_PROJECT/
    └── BOTB/
        ├── BOTB.sln
        ├── Assets/
        │   └── appicon.ico
        └── BOTB/
            ├── App.xaml
            ├── App.xaml.cs
            ├── MainWindow.xaml
            ├── MainWindow.xaml.cs
            ├── ArchiveWindow.cs
            ├── TelegramWindow.cs
            ├── BOTB.csproj
            └── Tools/
                └── 7zip/
```

**Technology:** C#, .NET 8, WPF/XAML, Windows Forms dialogs/hosting, 7-Zip, and Material Design packages. The application runs file processing asynchronously to keep the desktop interface responsive.

## 💡 Notes & Known Limitations

- **Windows only:** The app targets `net8.0-windows` and uses WPF.
- **Archive engine required:** The extractor expects `Tools/7zip/7z.exe` at runtime.
- **Telegram embedding is experimental:** It can interrupt input; use separate-window mode when needed.
- **Telegram executable path is machine-specific:** Update the path before distributing this source-based build.
- **No independent AI module confirmed:** The current checked source contains Telegram integration but no verified BOTB AI interface.
- **Privacy:** Keep local results and any Telegram session data out of public commits. The project's `.gitignore` excludes several sensitive file patterns, but users should still inspect changes before pushing.

## 👑 Credits

**Creator & Developer:** [Sindarksoul](https://github.com/sindarksoul)

**Third-party tooling:** [7-Zip](https://www.7-zip.org/) (see its bundled license notice), [Telegram Desktop](https://desktop.telegram.org/), and the .NET/WPF ecosystem.

## ⚖️ License & Responsible Use

BOTBv4's original project source is provided under the [MIT License](LICENSE). Bundled third-party tools and libraries retain their own licenses.

Use BOTB only with records, archives, and systems that you own or have permission to access. Do not publish private data, secrets, or extracted results.

## 💬 Feedback, Ideas & Bug Reports

Found a bug or have an improvement in mind? Open an [issue](https://github.com/sindarksoul/BOTBv4/issues) or explore the [repository](https://github.com/sindarksoul/BOTBv4).

---

<div align="center">

### ⚡ BestOfTheBest — Built to Keep Your Workflow Moving.

**Be the best. Use the best. BOTB.**

Made by **[@sindarksoul](https://github.com/sindarksoul)**

</div>
