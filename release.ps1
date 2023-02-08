#
# Powershell 7
#

param([String]$p="Windows", [String]$user="guest", [String]$pwd="", [String]$v="1.0.0")

$publishDir = ".\publish"

if (Test-Path $publishDir)
{
  Remove-Item -Path $publishDir -Recurse -Force
}

if ($p -like "Windows")
{
  $nugetPackages = $env:NUGET_PACKAGES ?? "$env:USERPROFILE\.nuget\packages"
  $squirrelExe = "$nugetPackages\clowd.squirrel\2.9.42\tools\Squirrel.exe"

  dotnet build `
    ".\src\Anteater.Intercom.Maui" `
    -f:net7.0-windows10.0.22621.0 `
    -c:Release `
    -o:$publishDir
  
  if (Test-Path "$publishDir\createdump.exe")
  {
    Remove-Item -Path "$publishDir\createdump.exe" -Force
  }

  & $squirrelExe pack `
    --allowUnaware `
    --releaseDir "\\10.0.1.100\Shared\AnteaterIntercom" `
    --packId "Anteater.Intercom" `
    --packVersion $v `
    --packAuthors "Anteater" `
    --packDir $publishDir `
    --icon "$publishDir\appicon.ico" `
    --splashImage "$publishDir\splashscreenSplashScreen.scale-100.png"
}

if ($p -like "iOS")
{

#    -p:EnableAssemblyILStripping=true `

  dotnet publish `
    ".\src\Anteater.Intercom.Maui" `
    -f:net7.0-ios `
    -c:Release `
    -r:ios-arm64 `
    -o:$publishDir `
    -p:ArchiveOnBuild=true `
    -p:CodesignKey="iPhone Distribution: Eugen Shvatsky (P5UCQD88QT)" `
    -p:CodesignProvision="com.anteater.intercom iOS Distribution Ad Hoc" `
    -p:CodesignEntitlements="Platforms\iOS\Entitlements.production.plist" `
    -p:ServerAddress=10.0.1.100 `
    -p:ServerUser=$user `
    -p:ServerPassword=$pwd `
    -p:_DotNetRootRemoteDirectory=/Users/$user/Library/Caches/Xamarin/XMA/SDKs/dotnet/

    Copy-Item -Path "$publishDir\Anteater.Intercom.Maui.ipa" -Destination "\\10.0.1.100\Shared\AnteaterIntercom\iOS" -Force
}
