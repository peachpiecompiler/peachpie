# A script to pack the compiled libraries to Nuget packages and install them to the global cache, forcing the update
# It is expected to be run from '/src/TheProjectToPackAndInstall' subfolder

# Note: In prior to use Powershell scripts, it might be needed to run:
# powershell Set-ExecutionPolicy Unrestricted -Scope CurrentUser

param([string]$rootDir, [string]$version, [string]$framework, [string]$configuration)

# Only do this once after the last framework is compiled
if ($framework -ne "netcoreapp1.0") {
    return
}

# The list of projects to process
$projects = @("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.MySql", "Peachpie.Library.MsSql", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.Compiler.Tools")
$suffix = "dev"

# We suppose the global package source is in the default location 
$packagesSource = (Resolve-Path "~/.nuget/packages").Path

# Create the Nuget packages and delete those currently installed
foreach ($project in $projects) {
    dotnet pack --no-build -c $configuration -o "$rootDir/.nugs" --version-suffix $suffix "$rootDir/src/$project"

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

# Reinstall the packages by restoring a dummy project that depends on them
dotnet restore "$rootDir/build/dummy"
