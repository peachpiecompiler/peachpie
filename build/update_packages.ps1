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
Write-Host "Root at" $rootDir
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

# produces nuget package of the project
function Pack
{
	param ([string]$project)
	$moreArgs = @()
	$projectDir = "$rootDir/src/$project"
	$target = if (Test-Path $projectDir/bin/$configuration/) { "pack" } else { "build" }
	# Do not pack full .NET 4.6 assemblies if they weren't produced
	if (!(Test-Path $projectDir/bin/$configuration/net4*))
	{
		$frameworks = Get-ChildItem "$projectDir/bin/$configuration" -Directory | % {$_.Name}
		$moreArgs += "/p:TargetFrameworks=" + ($a -join ';')
	}
	Write-Host "Building " $projectDir "/t:" $target " ..." -f green
	dotnet $target $defaultArgs $moreArgs $projectDir
}

# build & pack
Write-Host -f green "Building & packing additional packages ..."
@("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.Scripting", "Peachpie.Library.MySql", "Peachpie.Library.MsSql", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.NETCore.Web", "Peachpie.Compiler.Tools", "Peachpie.NET.Sdk") | % {Pack $_}

# Reinstall the packages by restoring a dummy project that depends on them
Write-Host -f green "Installing packages to nuget cache ..."
dotnet restore "$rootDir/build/dummy"
