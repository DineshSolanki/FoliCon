$ErrorActionPreference = 'Stop'
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageArgs = @{
  packageName   = 'folicon'
  unzipLocation = $toolsDir
  fileType      = 'zip'
  url64bit      = 'https://github.com/DineshSolanki/FoliCon/releases/download/V5.3.1/FoliCon-v5.3.1-x64.zip'
  checksum64    = '48510B29B40BD0D988C3EA889946E0C4D4D67A8D54FE5DACC412489EE72EC129'
  checksumType64= 'sha256'
}

Install-ChocolateyZipPackage @packageArgs

$target = Join-Path $toolsDir 'FoliCon.exe'
Install-ChocolateyShortcut -ShortcutFilePath "$([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Programs))\FoliCon.lnk" -TargetPath $target
