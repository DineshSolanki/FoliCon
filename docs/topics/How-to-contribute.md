# How to contribute

FoliCon is an open-source project, and we welcome contributions from the community. 
This guide explains how to contribute to FoliCon.

FoliCon has a lot of potential, and for that, **we need your help**.

## Before you start {collapsible="true"}

FoliCon is written in C# and uses WPF for the UI.
> 
> See the [Dependencies](Dependencies.md) page for more information about the libraries
>
{style="tip"}

Before building FoliCon, you'll need the following APIs:

* [A TMDB API](https://www.themoviedb.org/settings/api)
* [A IGDB API](https://api.igdb.com/)
* [DeviantArt API](https://www.deviantart.com/developers/register)

Then, you'll need to create a file called `AppConfig.json` in the `FoliCon` folder.
With the structure given below, you will be guided to it on first Run, so you can also skip manual creation.
```json
{
  "DevClientID": "Your_DeviantArt_Client_ID_here",
  "DevClientSecret": "Your_DeviantArt_Client_Secret_here",
  "TMDBKey": "Your_TMDB_API_here",
  "IgdbClientId": "Your_IGDB_Client_ID_here",
  "IgdbClientSecret": "Your_Client_Secret_API_here"
}
```

## Ways to contribute
* [Report a bug](https://github.com/DineshSolanki/FoliCon/issues/new/choose)
* [Fix an open bug](https://github.com/DineshSolanki/FoliCon/issues?q=is%3Aissue+is%3Aopen+sort%3Aupdated-desc)
* [Code contribution or feature addition](Code-Contribution.md)
* [Improve Documentation](Improve-Documentation.md)
* [Add new translations or improve existing ones](Translations.md)

## Discussion for feature requests and bugs
FoliCon uses GitHub issues to track bugs and feature requests.

Please search the existing issues before filing new issues to avoid duplicates.
FoliCon Feature discussions are done on the [Discussions](https://github.com/DineshSolanki/FoliCon/discussions/142) page.