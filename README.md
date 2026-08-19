[![-----------------------------------------------------](https://raw.githubusercontent.com/andreasbm/readme/master/assets/lines/aqua.png)](https://dineshsolanki.github.io/FoliCon-docs/)

<p align="center">
    <a href='https://dineshsolanki.github.io/FoliCon-docs/'> See FoliCon Docs</a>
</p>

[![-----------------------------------------------------](https://raw.githubusercontent.com/andreasbm/readme/master/assets/lines/aqua.png)](https://dineshsolanki.github.io/FoliCon-docs/)


<a href="https://dineshsolanki.github.io/FoliCon/">
    <img src="https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/folicon Icon.png" alt="FoliCon logo" title="FoliCon" align="right" height="120" />
</a>

# Folicon - The ultimate movie, show, music, games folder icon customizer
[![FoliCon Docs](https://img.shields.io/badge/docs-FoliCon-blue.svg)](https://dineshsolanki.github.io/FoliCon-docs/)
<img src="https://img.shields.io/github/commits-since/DineshSolanki/Folicon/latest/master"> <img src="https://img.shields.io/github/repo-size/dinesh-solanki/folicon.svg?logo=FoliconRepoSize"> <img src="https://img.shields.io/github/downloads/dineshsolanki/FoliCon/total?color=blue&style=plastic"> ![SourceForge Downloads](https://img.shields.io/sourceforge/dt/FoliCon)
 <img src="https://img.shields.io/github/last-commit/dinesh-solanki/folicon.svg?logo=FoliconLastCommit"> <img src="https://img.shields.io/github/issues/DineshSolanki/Folicon"> <img alt="GitHub Closed Issues" src="https://img.shields.io/github/issues-closed/DineshSolanki/FoliCon" /> ![Lines of code](https://sloc.xyz/github/DineshSolanki/FoliCon)

FoliCon is a folder icon changer for movies, TV shows, music, and games,anime,books. It fetches poster art and ratings from online databases, then applies them as folder icons. Built on WPF with a modular overlay plugin system so the look of the icon can be completely restyled without touching the codebase.

:star: Star us on GitHub — it helps!
## Star History

[![Star History Chart](https://api.star-history.com/svg?repos=DineshSolanki/FoliCon&type=Date)](https://star-history.com/#DineshSolanki/FoliCon&Date)
--
[OLD Repo](https://github.com/DineshSolanki/FoliCon/tree/f2cfc75414dcb8953793f2af833ed49fd496064e)

[<img height=100  alt="Download" src="https://user-images.githubusercontent.com/15937452/61147148-51575280-a4f9-11e9-953e-3989e58ed067.png" />](https://github.com/dinesh-solanki/Folicon/releases/latest) [![Download Folicon](https://a.fsdn.com/con/app/sf-download-button)](https://sourceforge.net/projects/folicon/files/latest/download)

<details>
  <summary>Screens (Click here to see) </summary>

![Before](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/before.png)
![After](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/after.jpg)
![Searching](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/searchingpro.jpg)
![PosterSearch](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/posterresult.jpg)
![DDownloading](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/downloading.png)
![Folicon](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/mainview.png)
![FoliConCustomIcon](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/customiconsetter.png)
</details>


## Getting Started
*No installation or additional libraries needed — download the latest release and run.*

FoliCon uses free API keys from third-party services (TMDB, IGDB/Twitch, DeviantArt). No credit card required — just create a free account on each service you want to use.

On first run, a setup wizard walks you through configuring the services you need. You can also access it anytime from **Settings → Setup Wizard**.

| Service | What it does | Key |
|---|---|---|
| **TMDB** | Movie & TV show metadata | [Get free key](https://www.themoviedb.org/settings/api) |
| **IGDB / Twitch** | Game metadata | [Create Twitch app](https://dev.twitch.tv/console/apps) |
| **DeviantArt** | Professional-mode icon searches | Just log in with your DeviantArt account |

All services are optional — configure only what you need.

## Overlay Plugin System

FoliCon's icon look is driven by **overlays** — JSON-defined packages of layers, images, and poster styling that can be swapped, created, and shared without recompiling the app.

Built-in overlays ship with the app. Community overlays can be installed from the **Overlay Store** or built from scratch with the **Overlay Designer**, which provides a live preview, layer ordering, colour and rotation controls, corner-radius editing, export and draft saving.

- **Overlay Store** — browse, install, update, and remove overlays from a community catalog
- **Overlay Designer** — create new overlays from templates, edit properties with a live preview, export installable packages, and submit them to the community store
- **Creating overlays** — full authoring guide: [CREATING-OVERLAYS.md](https://github.com/DineshSolanki/FoliCon-Overlays/blob/main/CREATING-OVERLAYS.md)
- **Community overlays repo** — [github.com/DineshSolanki/FoliCon-Overlays](https://github.com/DineshSolanki/FoliCon-Overlays)

## Localization
FoliCon supports English, Spanish, Arabic, Russian, Hindi, Portuguese, Japanese, and Chinese through [Crowdin](https://crowdin.com/project/folicon).

### Help with Translations
1. Visit our [Crowdin project](https://crowdin.com/project/folicon)
2. Sign up or log in to Crowdin
3. Select the language you want to help translate
4. Start translating strings

### For Developers
If you're working on the source code and want to manage translations:

1. Set up your Crowdin API credentials
   ```
   $env:CROWDIN_PROJECT_ID = "your_project_id"
   $env:CROWDIN_API_TOKEN = "your_api_token"
   ```

2. Use the provided PowerShell script for common operations:
   ```
   # Upload source files to Crowdin
   .\crowdin-sync.ps1 -Action upload-sources

   # Download latest translations
   .\crowdin-sync.ps1 -Action download
   ```

## Built With

- [The Movie Database](https://www.themoviedb.org/) — TV & movie metadata
- [DeviantArt](https://www.deviantart.com/) — professional-mode icon searches
- [IGDB](https://www.igdb.com/) — game metadata
- [Prism](https://github.com/PrismLibrary/Prism) — MVVM and modularity
- [HandyControls](https://github.com/ghost1372/HandyControls) — custom WPF controls
- [WinCopies.IconLib](https://github.com/avatars38/WinCopies) — icon creation
- [Ookii.Dialogs.Wpf](https://github.com/caioproiete/ookii-dialogs-wpf) — file dialogs
- [Sentry](https://sentry.io) — error tracking
- [NLog](https://nlog-project.org/) — logging
- [Crowdin](https://crowdin.com) — localization management

## Authors

* **Dinesh Solanki** - [Profile](https://github.com/dineshsolanki)

See also the list of [contributors](https://github.com/dineshsolanki/Folicon/graphs/contributors) who participated in this project.

## License
[GNU General Public License v3.0](https://github.com/DineshSolanki/FoliCon/blob/master/LICENSE)

## Acknowledgments

- A very Big thanks to [Jamedjo](https://github.com/Jamedjo) for His Project [Raticon](http://jamedjo.github.io/Raticon) which gave me a head Start.
- Thanks to [HazZbroGaminG](https://www.deviantart.com/hazzbrogaming), [Faelpessoal](https://www.deviantart.com/faelpessoal), and [Liaher](https://www.deviantart.com/liaher) for poster designs used in FoliCon overlays.
- <img height=80 alt="Powered By TMDB API" src="https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/tmdbblack.png" />
- This product uses the TMDb API but is not endorsed or certified by TMDb.
- All Professional Mode icons are fetched from publicly available galleries of DeviantArt, and all rights reserved to their respective owners.

---
Thank you to the following individuals who have provided invaluable inputs through discussions and issues:

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
