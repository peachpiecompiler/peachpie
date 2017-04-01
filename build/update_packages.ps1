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
@("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.Scripting", "Peachpie.Library.MySql", "Peachpie.Library.MsSql", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.NETCore.Web", "Peachpie.Compiler.Tools", "Peachpie.NET.Sdk") | % {
	$installedFolder = "$packagesSource/$_/$version-$suffix"
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
Write-Host -f green "Restoring packages ..."
@("Peachpie.NET.Sdk", "Peachpie.Library.Scripting", "Peachpie.NETCore.Web") | % {
	dotnet restore $defaultArgs "$rootDir/src/$_"
}

# produces nuget package of the project
Write-Host -f green "Building & packing additional packages ..."
function Pack {
	param ([string]$project)
	$projectDir = "$rootDir/src/$project"
	# check what is already built and just pack if possible
	$frameworks = if (Test-Path "$projectDir/bin/$configuration") { Get-ChildItem "$projectDir/bin/$configuration" -Directory | Where-Object {$_.GetFiles("*.dll").Count -gt 0} | % {$_.Name} } else { @() }
	$target = if ($frameworks.Count -eq 0) { "build" } else { "pack" }
	switch ($project)
	{
		"Peachpie.Compiler.Tools" { $framework = "netcoreapp1.0" }
		"Peachpie.Library.MySql" { $framework = "netstandard1.6" }
		"Peachpie.NETCore.Web" { $framework = "netstandard1.6" }
		"Peachpie.NET.Sdk" { $framework = "netcoreapp1.0" }
		default { $framework = "netstandard1.5" }
	}
	$moreArgs = "/p:TargetFramework=$framework"
	Write-Host "Building $projectDir /t:$target $moreArgs ..." -f green
	dotnet $target $defaultArgs $moreArgs $projectDir
}

# build & pack
@("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.Scripting", "Peachpie.Library.MySql", "Peachpie.Library.MsSql", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.NETCore.Web", "Peachpie.Compiler.Tools", "Peachpie.NET.Sdk") | % {Pack $_}

# Reinstall the packages by restoring a dummy project that depends on them
Write-Host -f green "Installing packages to nuget cache ..."
dotnet restore "$rootDir/build/dummy"
