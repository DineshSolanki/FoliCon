<img src="https://img.shields.io/github/commit-activity/m/DineshSolanki/FoliCon"> <img src="https://img.shields.io/github/repo-size/dinesh-solanki/folicon.svg?logo=FoliconRepoSize"> <img src="https://img.shields.io/github/commits-since/dinesh-solanki/folicon/v1.0.svg?logo=FoliconReleaseCommits"> <img src="https://img.shields.io/github/last-commit/dinesh-solanki/folicon.svg?logo=FoliconLastCommit"> <img src="https://img.shields.io/github/issues/DineshSolanki/Folicon">

# Folicon
Creates Folder icons for Movies and Serials, inspired and derived From [Raticon](https://github.com/Jamedjo/Raticon)

This Application Converts your Boring Movie, TV, Game Folder Icons to A good Looking and informative icons, Which includes their Rating too.
The Application is WIP, but usable. 

[<img height=100  alt="Download" src="https://user-images.githubusercontent.com/15937452/61147148-51575280-a4f9-11e9-953e-3989e58ed067.png" />](https://github.com/dinesh-solanki/Folicon/releases/latest) [![Download Folicon](https://a.fsdn.com/con/app/sf-download-button)](https://sourceforge.net/projects/folicon/files/latest/download)

<details>
  <summary>Screens (Click here to see) </summary>
    
![Before](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon%20v1.0/Before.png)
![After](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon%20v1.0/After.png)
![Searching](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon%20v1.0/Searching.png)
![Description](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon%20v1.0/FoliconSS%201.png)
![Description](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon%20v1.0/Description.png)
![Description](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon%20v1.0/FoliconSS%202.png)
![Description](https://github.com/dinesh-solanki/Project-Assets/blob/master/Folicon%20v1.0/FoliconSS%204.png)
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
   <add key="GoogleAPI" value="Your_Google_API_here"/>
  </appSettings>
</configuration>
```
### Prerequisites
A TMDB API [Get it](https://www.themoviedb.org/settings/api)

A GOOGLE API [Get it](https://developers.google.com/maps/documentation/javascript/get-api-key#get-the-api-key)

## Built With

* [The Movie Database](https://www.themoviedb.org/) - Powered by TMDB API
* Google Custom Search
* [IconLib](https://www.codeproject.com/Articles/16178/IconLib-Icons-Unfolded-MultiIcon-and-Windows-Vista) - To make Icons from viewModel
* [Extended WPF Toolkit™](https://github.com/xceedsoftware/wpftoolkit) - For Custom Controls
* [Ookii.Dialogs.Wpf](https://github.com/caioproiete/ookii-dialogs-wpf) - For File Dialogs

## Authors

* **Dinesh Solanki** - [Profile](https://github.com/dineshsolanki)

See also the list of [contributors](https://github.com/dineshsolanki/Folicon/graphs/contributors) who participated in this project.

## License



## Acknowledgments

* A very Big thanks to [Jamedjo](https://github.com/Jamedjo) for His Project [Raticon](http://jamedjo.github.io/Raticon) which gave me a head Start, and throughout this Project, His code and Design is used.
* <img height=80 alt="Powered By TMDB API" src="https://github.com/dinesh-solanki/Folicon/blob/master/Folicon_Native/Resources/TMDB%20black%20logo.png" />
* This product uses the TMDb API but is not endorsed or certified by TMDb.

