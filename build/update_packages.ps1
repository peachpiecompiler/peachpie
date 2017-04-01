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
$projects = @("Peachpie.Runtime", "Peachpie.Library", "Peachpie.Library.MySql", "Peachpie.Library.MsSql", "Peachpie.App", "Peachpie.CodeAnalysis", "Peachpie.NETCore.Web", "Peachpie.Compiler.Tools", "Peachpie.NET.Sdk")
$suffix = "dev"

# We suppose the global package source is in the default location 
$packagesSource = (Resolve-Path "~/.nuget/packages").Path

# Create the Nuget packages and delete those currently installed
foreach ($project in $projects) {
    $appendedArgs = New-Object System.Collections.Generic.List[System.String]
    
    # Do not pack full .NET 4.6 assemblies if they weren't produced
    $projectDir = "$rootDir/src/$project"
    #if (!(Test-Path $projectDir/bin/$configuration/net46/*)) {
    #    $packFramework = if ($project -eq "Peachpie.Compiler.Tools") { "netcoreapp1.0" } else { "netstandard1.6" }
    #    $appendedArgs.Add("/p:TargetFrameworks=")
    #    $appendedArgs.Add("/p:TargetFramework=$packFramework")
    #}

    dotnet pack --no-build -c $configuration --version-suffix $suffix $projectDir $appendedArgs

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
