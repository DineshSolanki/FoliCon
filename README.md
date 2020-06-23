<a href="https://dineshsolanki.github.io/FoliCon/">
    <img src="https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/folicon Icon.png" alt="FoliCon logo" title="FoliCon" align="right" height="120" />
</a>

# Folicon - The ultimate movie, show, music, games folder icon customizer

<img src="https://img.shields.io/github/commits-since/DineshSolanki/Folicon/latest/master"> <img src="https://img.shields.io/github/repo-size/dinesh-solanki/folicon.svg?logo=FoliconRepoSize"> <img src="https://img.shields.io/github/downloads/dineshsolanki/FoliCon/total?color=blue&style=plastic"> <img src="https://img.shields.io/github/last-commit/dinesh-solanki/folicon.svg?logo=FoliconLastCommit"> <img src="https://img.shields.io/github/issues/DineshSolanki/Folicon">

Folicon is a Folder icon changer which works for movie,music, games, and shows, it also shows rating on the created icons, and has two mode, POSTER and Professional, inspired From [Raticon](https://github.com/Jamedjo/Raticon)

:star: Star us on GitHub — it helps!

[<img height=100  alt="Download" src="https://user-images.githubusercontent.com/15937452/61147148-51575280-a4f9-11e9-953e-3989e58ed067.png" />](https://github.com/dinesh-solanki/Folicon/releases/latest) [![Download Folicon](https://a.fsdn.com/con/app/sf-download-button)](https://sourceforge.net/projects/folicon/files/latest/download)

<details>
  <summary>Screens (Click here to see) </summary>
    
![Before](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/before.png)
![After](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/after.jpg)
![Searching](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/searchingpro.jpg)
![Description](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/posterresult.jpg)
![Description](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/downloading.png)
![Description](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon/mainview.png)
</details>


## Getting Started
*To Use this Application Instantly, Click the Download button Above or Go to "Release", and start using. (No Installation or Additional Libraries needed)*

To compile this Source you need to Create "App.config" file with structure given below
```
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2" />
    </startup>
  <appSettings>
   <add key="TMDBAPI" value="Your_TMDB_API_here"/>
    <add key="IGDBAPI" value="Your_IGDB_API_here" />
    <add key="DeviantClientSecret" value="Your_DeviantArt_Client_Secret_here" />
    <add key="DeviantClientId" value="Your_DeviantArt_Client_ID_here" />
    <add key="Token" value="" />
  </appSettings>
</configuration>
```
### Prerequisites (these are for compiling the source, to only use the application, you can download latest release)
A TMDB API [Get it](https://www.themoviedb.org/settings/api)

A IGDB API [Get it](https://api.igdb.com/)

DeviantArt API [Get it](https://www.deviantart.com/developers/register)

## Built With

* [The Movie Database](https://www.themoviedb.org/) - TV, Movies searching
* [DeviantArt](https://www.deviantart.com/) - Professional Mode searching
* [IGDB](https://www.igdb.com/) - Games searching
* [IconLib](https://www.codeproject.com/Articles/16178/IconLib-Icons-Unfolded-MultiIcon-and-Windows-Vista) - To make Icons from viewModel
* [Extended WPF Toolkit™](https://github.com/xceedsoftware/wpftoolkit) - For Custom Controls
* [Ookii.Dialogs.Wpf](https://github.com/caioproiete/ookii-dialogs-wpf) - For File Dialogs

## Authors

* **Dinesh Solanki** - [Profile](https://github.com/dineshsolanki)

See also the list of [contributors](https://github.com/dineshsolanki/Folicon/graphs/contributors) who participated in this project.

## License
[GNU General Public License v3.0](https://github.com/DineshSolanki/FoliCon/blob/master/LICENSE)

## Acknowledgments

* A very Big thanks to [Jamedjo](https://github.com/Jamedjo) for His Project [Raticon](http://jamedjo.github.io/Raticon) which gave me a head Start.
* <img height=80 alt="Powered By TMDB API" src="https://github.com/dinesh-solanki/Folicon/blob/master/Folicon_Native/Resources/TMDB%20black%20logo.png" />
* This product uses the TMDb API but is not endorsed or certified by TMDb.
* All Professional Mode icons are fetched from publicly available galaries of DeviantArt, and all rights reserves to their respective owners.

