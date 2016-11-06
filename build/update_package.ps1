# A script to pack the compiled library to a Nuget package and install it to the global cache, forcing the update
# It is expected to be run from '/src/TheProjectToPackAndInstall' subfolder

# Note: In prior to use Powershell scripts, it might be needed to run:
# powershell Set-ExecutionPolicy Unrestricted -Scope CurrentUser

param([string]$project, [string]$version, [string]$framework, [string]$configuration)

# Only do this once after the last framework is compiled
if ($framework -ne "netcoreapp1.0") {
    return
}

# Distinguish between Windows and other OSs (variable $IsWindows is not present in Desktop version)
$IsWindowsHlp = $true
if ($PSEdition -eq "Core") {
    # If running in Powershell Core, we might be on a different platform
    $IsWindowsHlp = $IsWindows
}

# Launch nuget.exe either directly or under Mono
if ($IsWindowsHlp) {
    $nugetCommand = "../../tools/nuget.exe"
    $firstArg = @()
} else {
    # We expect Mono to be installed
    $nugetCommand = "mono"
    $firstArg = @("../../tools/nuget.exe")
}

# We suppose the global package source is in the default location 
$packagesSource = (Resolve-Path "~/.nuget/packages").Path

# Create the Nuget package and overwrite the one that is currently installed
# TODO: Solve the problem with the lowercase names on Linux
dotnet pack --no-build -c $configuration -o ../../.nugs --version-suffix beta
& $nugetCommand $firstArg delete $project $version-beta -Source $packagesSource -Noninteractive
& $nugetCommand $firstArg add ../../.nugs/$project.$version-beta.nupkg -Source $packagesSource -Expand -Noninteractive
