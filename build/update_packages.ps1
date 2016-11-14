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
$projects = @("Peachpie.Runtime", "Peachpie.Library", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.Compiler.Tools")

# Distinguish between Windows and other OSs (variable $IsWindows is not present in Desktop version)
$IsWindowsHlp = $true
if ($PSEdition -eq "Core") {
    # If running in Powershell Core, we might be on a different platform
    $IsWindowsHlp = $IsWindows
}

# If later needed, launch nuget.exe either directly or under Mono
$nugetCommand = $null
$prependArgs = @()
if ($IsWindowsHlp) {
    $nugetCommand = "$rootDir/tools/nuget.exe"
} else {
    # Check if is Mono installed
    if (Get-Command "mono" -ErrorAction SilentlyContinue) {
        $nugetCommand = "mono"
        $prependArgs.Add("$rootDir/tools/nuget.exe")
    } else {
        "Mono not found, packages will not be reinstalled"
    }
}

# We suppose the global package source is in the default location 
$packagesSource = (Resolve-Path "~/.nuget/packages").Path

# Create the Nuget packages and overwrite those currently installed
foreach ($project in $projects) {
    dotnet pack --no-build -c $configuration -o "$rootDir/.nugs" --version-suffix beta "$rootDir/src/$project"

    if ($nugetCommand) {
        # TODO: Solve the problem with the lowercase names on Linux
        & $nugetCommand $prependArgs delete $project "$version-beta" -Source $packagesSource -Noninteractive
        & $nugetCommand $prependArgs add "$rootDir/.nugs/$project.$version-beta.nupkg" -Source $packagesSource -Expand -Noninteractive
    }
}
