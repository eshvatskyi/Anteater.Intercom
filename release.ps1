#
# Powershell 7
#

param([String]$v="1.0.0") 

$assetsDir = ".\src\Anteater.Intercom.WinUI\Assets"
$publishDir = ".\publish"

$nugetPackages = $env:NUGET_PACKAGES ?? "$env:USERPROFILE\.nuget\packages"
$squirrelExe = "$nugetPackages\clowd.squirrel\2.9.42\tools\Squirrel.exe"

if (Test-Path $publishDir) {
  Remove-Item -Path $publishDir -Recurse -Force
}

dotnet publish `
  ".\src\Anteater.Intercom.WinUI" `
  /p:Platform=x86 `
  --self-contained `
  -c Release `
  -r win-x86 `
  -f "net7.0-windows10.0.22621.0" `
  -o $publishDir

if (Test-Path "$publishDir\createdump.exe") {
  Remove-Item -Path "$publishDir\createdump.exe" -Force
}

& $squirrelExe pack `
  --allowUnaware `
  --releaseDir "\\10.0.1.100\Shared\AnteaterIntercom" `
  --packId "Anteater.Intercom" `
  --packVersion $v `
  --packAuthors "Anteater" `
  --packDir $publishDir `
  --icon "$assetsDir\Icon.ico" `
  --splashImage "$assetsDir\SplashScreen.png"
