<div align="center">

<a href="https://dineshsolanki.github.io/FoliCon/">
  <img src="https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/folicon Icon.png" alt="FoliCon Logo" width="128" height="128" />
</a>

# FoliCon

### The Ultimate Movie, TV Series, Anime, Music & Game Folder Icon Customizer for Windows

[![GitHub Release](https://img.shields.io/github/v/release/DineshSolanki/FoliCon?color=blue&logo=github)](https://github.com/DineshSolanki/FoliCon/releases/latest)
[![WinGet Package](https://img.shields.io/badge/winget-DineshSolanki.FoliCon-0078D7?logo=windows&logoColor=white)](https://github.com/microsoft/winget-pkgs/tree/master/manifests/d/DineshSolanki/FoliCon)
[![Chocolatey](https://img.shields.io/badge/chocolatey-folicon-00A4EF?logo=chocolatey&logoColor=white)](https://community.chocolatey.org/packages/folicon)
[![Total GitHub Downloads](https://img.shields.io/github/downloads/dineshsolanki/FoliCon/total?color=2ea44f&logo=github)](https://github.com/DineshSolanki/FoliCon/releases)
[![SourceForge Downloads](https://img.shields.io/sourceforge/dt/FoliCon?color=orange&logo=sourceforge)](https://sourceforge.net/projects/folicon/)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://github.com/DineshSolanki/FoliCon/blob/master/LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows)](https://dineshsolanki.github.io/FoliCon/)
[![Crowdin](https://badges.crowdin.net/folicon/localized.svg)](https://crowdin.com/project/folicon)
[![Docs](https://img.shields.io/badge/docs-FoliCon--docs-success?logo=gitbook)](https://dineshsolanki.github.io/FoliCon-docs/)

<p align="center">
  <a href="#-quick-installation"><b>Quick Install</b></a> •
  <a href="#-key-features"><b>Features</b></a> •
  <a href="#-before--after"><b>Visual Showcase</b></a> •
  <a href="#-overlay-plugin-system"><b>Overlay Store</b></a> •
  <a href="#-getting-started"><b>Setup Guide</b></a> •
  <a href="https://dineshsolanki.github.io/FoliCon-docs/"><b>Documentation</b></a>
</p>

</div>

---

**FoliCon** is a modern, high-performance Windows application that transforms plain, generic folders into stunning, personalized media artworks. It automatically queries online databases (TMDB, IGDB, DeviantArt) for high-resolution posters, fan art, and ratings (IMDb, TMDB), dynamically renders them into native `.ico` files, and applies them instantly without restarting Windows Explorer.

Built with **WPF & .NET**, FoliCon features an extensible **Overlay Plugin System** and built-in **Overlay Designer**, giving you 100% creative freedom to customize shapes, 3D borders, reflections, and rating badges.

---

## ⚡ Quick Installation

Choose your preferred way to install FoliCon:

### Method 1: Windows Package Manager (`winget`)
```powershell
winget install DineshSolanki.FoliCon
```

### Method 2: Chocolatey
```powershell
choco install folicon
```

### Method 3: Direct Portable Download
Download the latest pre-compiled archive from [**GitHub Releases**](https://github.com/dinesh-solanki/Folicon/releases/latest) or [**SourceForge**](https://sourceforge.net/projects/folicon/files/latest/download).
*No installer needed — simply unzip and run `FoliCon.exe`.*

---

## 📸 Before & After

<div align="center">
  <table>
    <tr>
      <td align="center"><b>Plain Windows Folders (Before)</b></td>
      <td align="center"><b>FoliCon Customized Folders (After)</b></td>
    </tr>
    <tr>
      <td><img src="https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/before.png" alt="Before FoliCon" width="420" /></td>
      <td><img src="https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/after.jpg" alt="After FoliCon" width="420" /></td>
    </tr>
  </table>
</div>

<details>
  <summary><b>🔍 Click here to expand more UI Screenshots & Feature Views</b></summary>
  <br/>

  | Searching & Matching | Poster Selection & IMDb Badges |
  |:---:|:---:|
  | ![Searching](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/searchingpro.jpg) | ![PosterSearch](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/posterresult.jpg) |

  | Main Batch Workflow | Custom Manual Icon Setter |
  |:---:|:---:|
  | ![Folicon Main View](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/mainview.png) | ![FoliCon Custom Icon](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/customiconsetter.png) |

</details>

---

## ✨ Key Features

* 🎬 **Multi-Media Metadata Support**: Works seamlessly across **Movies**, **TV Series / Shows**, **Anime**, **Music Albums**, **PC/Console Games**, and **Books**.
* 🌟 **Live Rating Badges**: Embed official **IMDb** & **TMDB** rating stars directly on your folder art.
* 🖼️ **Dual Operation Modes**:
  * **Poster Mode**: Automatically extracts posters, fanart, release years, and story overviews from TMDB.
  * **Professional Mode**: Fetches curated, custom-designed icons from top designers on DeviantArt.
* 🔄 **Instant Shell Refresh**: Uses native Windows Shell APIs (`SHFOLDERCUSTOMSETTINGS` / `Desktop.ini`) to refresh icon caches on the fly with zero Explorer restarts or lag.
* 🧩 **Extensible Overlay System**: Choose from dozens of community styles (Blu-ray cases, DVD cases, Clear Glass, Minimalist, Rounded, Perspective 3D).
* 🎨 **Integrated Overlay Designer**: Create and customize your own overlay styles with a real-time WYSIWYG editor and export them as shareable packages.
* 🌍 **Multilingual**: Translated into 8+ languages (English, Spanish, Arabic, Russian, Hindi, Portuguese, Japanese, Chinese).
* 🪶 **100% Lightweight & Portable**: Zero bloat, zero background telemetry, and no registry clutter.

---

## 🧩 Overlay Plugin System & Store

FoliCon separates icon rendering from core code using JSON-driven **Overlays**.

* 🛍️ **Overlay Store**: Browse, install, update, and rate overlays created by the community.
* 🎨 **Overlay Designer**: Build custom overlays from scratch or templates — adjust aspect ratios, rotations, opacity, drop-shadows, and corner radiuses with instant live preview.
* 📖 **Authoring Guide**: Learn how to create your own overlays in [CREATING-OVERLAYS.md](https://github.com/DineshSolanki/FoliCon-Overlays/blob/main/CREATING-OVERLAYS.md).
* 🌐 **Community Catalog**: [github.com/DineshSolanki/FoliCon-Overlays](https://github.com/DineshSolanki/FoliCon-Overlays)

---

## 🚀 Getting Started

*No complex installation required — download the latest release and run.*

FoliCon connects with third-party media APIs for accurate metadata. On your first run, a friendly **Setup Wizard** will guide you through connecting the services you wish to use (*all services are 100% free*):

| Service | Purpose | How to Get (Free) |
| :--- | :--- | :--- |
| **TMDB** | Movie & TV Show Posters, Overviews, IMDb/TMDB Ratings | [Get free API key](https://www.themoviedb.org/settings/api) |
| **IGDB / Twitch** | Video Game Covers, Release Dates & Metadata | [Create free Twitch App](https://dev.twitch.tv/console/apps) |
| **DeviantArt** | Professional Mode Curated Artist Icons | Log in with any DeviantArt account |

> [!NOTE]
> All services are optional. You can configure only the providers you need from **Settings → Setup Wizard**.

---

## 🌐 Localization & Translations

Help us bring FoliCon to more languages! Localization is powered by [Crowdin](https://crowdin.com/project/folicon).

1. Visit the [FoliCon Crowdin Project](https://crowdin.com/project/folicon).
2. Select your language and start translating strings.

### For Developers (Syncing Translations)
```powershell
$env:CROWDIN_PROJECT_ID = "your_project_id"
$env:CROWDIN_API_TOKEN  = "your_api_token"

# Upload latest source strings
.\crowdin-sync.ps1 -Action upload-sources

# Download translated language packs
.\crowdin-sync.ps1 -Action download
```

---

## 🛠️ Built With

* [.NET & WPF](https://dotnet.microsoft.com/) — High-performance native Windows desktop framework
* [Prism](https://github.com/PrismLibrary/Prism) — MVVM architecture & modularity
* [HandyControls](https://github.com/ghost1372/HandyControls) — Modern WPF control suite
* [WinCopies.IconLib](https://github.com/avatars38/WinCopies) — High-quality multi-resolution `.ico` encoder
* [Ookii.Dialogs.Wpf](https://github.com/caioproiete/ookii-dialogs-wpf) — Native Windows folder selection dialogs
* [The Movie Database (TMDb) API](https://www.themoviedb.org/) — Entertainment metadata
* [IGDB API](https://www.igdb.com/) — Game metadata
* [DeviantArt API](https://www.deviantart.com/) — Community icon galleries
* [Sentry](https://sentry.io) & [NLog](https://nlog-project.org/) — Diagnostics & logging

---

## 📈 Star History

[![Star History Chart](https://api.star-history.com/svg?repos=DineshSolanki/FoliCon&type=Date)](https://star-history.com/#DineshSolanki/FoliCon&Date)

---

## 👥 Contributors & Acknowledgments

FoliCon is created and maintained by **[Dinesh Solanki](https://github.com/dineshsolanki)**.

### Acknowledgments:
* A big tribute to [Jamedjo](https://github.com/Jamedjo) for **[Raticon](http://jamedjo.github.io/Raticon)**, the original project that inspired the creation of FoliCon.
* Poster & frame designs by [HazZbroGaminG](https://www.deviantart.com/hazzbrogaming), [Faelpessoal](https://www.deviantart.com/faelpessoal), and [Liaher](https://www.deviantart.com/liaher).
* This product uses the TMDb API but is not endorsed or certified by TMDb.
* Professional Mode icons are fetched from publicly available galleries on DeviantArt; all rights remain with their respective creators.

### Special Community Contributors:
<table>
  <tr>
    <td align="center"><a href="https://github.com/pierx"><img src="https://github.com/pierx.png?size=50" alt="pierx" /><br /><sub><b>@pierx</b></sub></a></td>
    <td align="center"><a href="https://github.com/FazCodeFR"><img src="https://github.com/FazCodeFR.png?size=50" alt="FazCodeFR" /><br /><sub><b>@FazCodeFR</b></sub></a></td>
    <td align="center"><a href="https://github.com/TheFmC"><img src="https://github.com/TheFmC.png?size=50" alt="TheFmC" /><br /><sub><b>@TheFmC</b></sub></a></td>
    <td align="center"><a href="https://github.com/MasoudRahmani"><img src="https://github.com/MasoudRahmani.png?size=50" alt="MasoudRahmani" /><br /><sub><b>@MasoudRahmani</b></sub></a></td>
    <td align="center"><a href="PoetaGA"><img src="https://github.com/PoetaGA.png?size=50" alt="PoetaGA" /><br /><sub><b>@PoetaGA</b></sub></a></td>
  </tr>
</table>
... and many more who have interacted with us through issues and discussions.
---

## 📄 License

This project is licensed under the **[GNU General Public License v3.0](https://github.com/DineshSolanki/FoliCon/blob/master/LICENSE)**.

<img height=80 alt="Powered By TMDB API" src="https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/tmdbblack.png" />
