#
# Powershell 7
#

param([String]$p = "Windows", [String]$user = "guest", [String]$pwd = "", [String]$v = "1.0.0")

$publishDir = ".\publish"

if (Test-Path $publishDir)
{
    Remove-Item -Path $publishDir -Recurse -Force
}

if ($p -like "Windows")
{
    $nugetPackages = $env:NUGET_PACKAGES ?? "$env:USERPROFILE\.nuget\packages"
    $squirrelExe = "$nugetPackages\clowd.squirrel\2.9.42\tools\Squirrel.exe"

    dotnet publish ".\src\App" `
        -f:net8.0-windows10.0.22621.0 `
        -c:Release `
        -o:$publishDir
  
    if (Test-Path "$publishDir\createdump.exe") { Remove-Item -Path "$publishDir\createdump.exe" -Force }

    if (!$?) { Exit $LASTEXITCODE }

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
    dotnet publish ".\src\App" `
        -f:net8.0-ios `
        -c:Release `
        -p:ServerAddress=10.0.1.100 `
        -p:ServerUser=$user `
        -p:ServerPassword=$pwd `
        -p:_DotNetRootRemoteDirectory=/Users/$user/Library/Caches/Xamarin/XMA/SDKs/dotnet/
    #    -p:EnableAssemblyILStripping=true `

    if (!$?) { Exit $LASTEXITCODE }

    Copy-Item -Path ".\src\App\bin\Release\net8.0-ios\ios-arm64\publish\Anteater.Intercom.ipa" -Destination "\\10.0.1.100\Shared\AnteaterIntercom\iOS" -Force
}
