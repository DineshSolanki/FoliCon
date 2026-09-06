Continue = 'Stop'
 = "$nuspec = @(
"<?xml version=""1.0"" encoding=""utf-8""?>",
"<package xmlns=""http:\\schemas.microsoft.com\packaging\2015\06\nuspec.xsd"">",
"  <metadata>",
"    <id>folicon<\id>",
"    <version>5.3.1<\version>",
"    <title>FoliCon<\title>",
"    <authors>Dinesh Solanki<\authors>",
"    <projectUrl>https:\\dineshsolanki.github.io\FoliCon\<\projectUrl>",
"    <projectSourceUrl>https:\\github.com\DineshSolanki\FoliCon<\projectSourceUrl>",
"    <packageSourceUrl>https:\\github.com\DineshSolanki\FoliCon\tree\master\distribution\chocolatey<\packageSourceUrl>",
"    <docsUrl>https:\\dineshsolanki.github.io\FoliCon-docs\<\docsUrl>",
"    <bugTrackerUrl>https:\\github.com\DineshSolanki\FoliCon\issues<\bugTrackerUrl>",
"    <licenseUrl>https:\\raw.githubusercontent.com\DineshSolanki\FoliCon\master\LICENSE<\licenseUrl>",
"    <requireLicenseAcceptance>false<\requireLicenseAcceptance>",
"    <iconUrl>https:\\raw.githubusercontent.com\dinesh-solanki\Project-Assets\master\Folicon\folicon%20Icon.png<\iconUrl>",
"    <tags>folicon folder-icons customization icons movie-icons plex jellyfin anime games imdb tmdb<\tags>",
"    <summary>Automated Movie, TV Show, Game, Anime &amp; Music Folder Icon Customizer with IMDb ratings<\summary>",
"    <description>FoliCon is an open-source Windows folder icon customizer for movies, TV series, anime, games, and music. It connects to TMDB, IGDB, and DeviantArt to automatically fetch high-resolution posters and ratings, compiling them into stylized folder icons in real time.<\description>",
"    <releaseNotes>https:\\github.com\DineshSolanki\FoliCon\releases\tag\V5.3.1<\releaseNotes>",
"  <\metadata>",
"  <files>",
"    <file src=""tools\**"" target=""tools"" \>",
"  <\files>",
"<\package>"
)
$nuspec | Set-Content -Path "distribution\chocolatey\folicon.nuspec"

$install = @(
"$ErrorActionPreference = 'Stop'",
"$toolsDir = ""$(Split-Path -parent $MyInvocation.MyCommand.Definition)""",
"$packageArgs = @{",
"  packageName   = 'folicon'",
"  unzipLocation = $toolsDir",
"  fileType      = 'zip'",
"  url64bit      = 'https:\\github.com\DineshSolanki\FoliCon\releases\download\V5.3.1\FoliCon-v5.3.1-x64.zip'",
"  checksum64    = '48510B29B40BD0D988C3EA889946E0C4D4D67A8D54FE5DACC412489EE72EC129'",
"  checksumType64= 'sha256'",
"}",
"Install-ChocolateyZipPackage @packageArgs"
)
$install | Set-Content -Path "distribution\chocolatey\tools"
 = @{
  packageName   = 'folicon'
  unzipLocation = 
  fileType      = 'zip'
  url64bit      = 'https://github.com/DineshSolanki/FoliCon/releases/download/V5.3.1/FoliCon-v5.3.1-x64.zip'
  checksum64    = '48510B29B40BD0D988C3EA889946E0C4D4D67A8D54FE5DACC412489EE72EC129'
  checksumType64= 'sha256'
}
Install-ChocolateyZipPackage @packageArgs
