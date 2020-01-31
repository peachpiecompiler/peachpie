# A script to update the NuGet cache with built packages

# Note: In prior to use Powershell scripts, it might be needed to run:
# powershell Set-ExecutionPolicy Unrestricted -Scope CurrentUser

param([string]$version = "1.0.0", [string]$suffix = "dev")

# We suppose the global package source is in the default location 
$rootDir = [System.IO.Path]::GetFullPath("$PSScriptRoot/..")
Write-Host "Root at" $rootDir
$packagesSource = (Resolve-Path "~/.nuget/packages").Path
$defaultArgs = "/p:VersionPrefix=$version,VersionSuffix=$suffix"

## Delete old nuget packages
Write-Host -f green "Deleting '$version-$suffix' packages from '$packagesSource' ..."
@("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.Scripting", "Peachpie.Library.MySql", "Peachpie.Library.MsSql", "Peachpie.Library.Graphics", "Peachpie.Library.Network", "Peachpie.Library.PDO", "Peachpie.Library.XmlDom", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.AspNetCore.Web", "Peachpie.AspNetCore.Mvc", "Peachpie.NET.Sdk", "Peachpie.Library.PDO.MySql", "Peachpie.Library.PDO.Sqlite", "Peachpie.Library.SqlSrv") | % {
	$installedFolder = "$packagesSource/$_/$version-$suffix"
    if (Test-Path $installedFolder) {
        Remove-Item -Recurse -Force $installedFolder
    }
}

## Clean up the installed tool settings so that no old dependencies hang in there 
#$toolFolder = "$packagesSource/.tools/Peachpie.Compiler.Tools/$version-$suffix"
#if (Test-Path $toolFolder) {
#    Remove-Item -Recurse -Force $toolFolder
#}

### Restore top packages, dependencies restored recursively
#Write-Host -f green "Restoring packages ..."
#@("Peachpie.NET.Sdk", "Peachpie.Library.Scripting", "Peachpie.AspNetCore.Web") | % {
#	dotnet restore $defaultArgs "$rootDir/src/$_"
#}

# Reinstall the packages by restoring a dummy project that depends on them
Write-Host -f green "Installing packages to nuget cache ..."
dotnet restore "$rootDir/build/dummy"
