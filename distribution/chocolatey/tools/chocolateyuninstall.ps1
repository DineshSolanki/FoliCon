$ErrorActionPreference = 'Stop'
$shortcut = "$([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::Programs))\FoliCon.lnk"
if (Test-Path $shortcut) {
  Remove-Item -Force $shortcut
}
