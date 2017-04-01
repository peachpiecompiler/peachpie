# A script to pack the compiled libraries to Nuget packages and install them to the global cache, forcing the update
# It is expected to be run from '/src/TheProjectToPackAndInstall' subfolder

# Note: In prior to use Powershell scripts, it might be needed to run:
# powershell Set-ExecutionPolicy Unrestricted -Scope CurrentUser

param([string]$rootDir, [string]$version, [string]$framework, [string]$configuration)

# Only do this once after the last framework is compiled
if ($framework -ne "netcoreapp1.0") {
    return
}

# We suppose the global package source is in the default location 
$rootDir = [System.IO.Path]::GetFullPath($rootDir)
$packagesSource = (Resolve-Path "~/.nuget/packages").Path
$suffix = "dev"
$defaultArgs = @("/p:Configuration=$configuration", "/p:VersionSuffix=$suffix")

## Delete old nuget packages
Write-Host -f green "Deleting '$suffix' packages from '$packagesSource' ..."
$projects = @("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.Scripting", "Peachpie.Library.MySql", "Peachpie.Library.MsSql", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.NETCore.Web", "Peachpie.Compiler.Tools", "Peachpie.NET.Sdk")
foreach ($project in $projects) {
    $installedFolder = "$packagesSource/$project/$version-$suffix"
    if (Test-Path $installedFolder) {
        Remove-Item -Recurse -Force $installedFolder
    }
}

# Clean up the installed tool settings so that no old dependencies hang in there 
$toolFolder = "$packagesSource/.tools/Peachpie.Compiler.Tools/$version-$suffix"
if (Test-Path $toolFolder) {
    Remove-Item -Recurse -Force $toolFolder
}

## Restore top packages, dependencies restored recursively

$projects = @("Peachpie.NET.Sdk", "Peachpie.Library.Scripting", "Peachpie.NETCore.Web");
foreach ($project in $projects) {
	Write-Host -f green "Restoring '$rootDir/src/$project'"
    dotnet restore $defaultArgs "$rootDir/src/$project"
}

# build & pack
Write-Host -f green "Building & packing additional packages ..."
dotnet build $defaultArgs "/p:TargetFramework=netstandard1.5" $rootDir/src/Peachpie.CodeAnalysis
dotnet build $defaultArgs "/p:TargetFramework=netstandard1.5" $rootDir/src/Peachpie.Library
dotnet build $defaultArgs "/p:TargetFramework=netstandard1.5" $rootDir/src/Peachpie.Library.Scripting
dotnet build $defaultArgs "/p:TargetFramework=netstandard1.6" $rootDir/src/Peachpie.NETCore.Web
dotnet build $defaultArgs "/p:TargetFramework=netcoreapp1.0" $rootDir/src/Peachpie.NET.Sdk
Write-Host -f green "Packing ..."
$defaultArgs += "--no-build" #/t:pack
dotnet pack $defaultArgs "/p:TargetFramework=netstandard1.5" $rootDir/src/Peachpie.Runtime
dotnet pack $defaultArgs "/p:TargetFramework=netstandard1.6" $rootDir/src/Peachpie.Library.MySql
dotnet pack $defaultArgs "/p:TargetFramework=netstandard1.5" $rootDir/src/Peachpie.Library.MsSql
dotnet pack $defaultArgs "/p:TargetFramework=netstandard1.5" $rootDir/src/Peachpie.App

# Reinstall the packages by restoring a dummy project that depends on them
Write-Host -f green "Installing packages to nuget cache ..."
dotnet restore "$rootDir/build/dummy"
