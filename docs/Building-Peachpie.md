The solution consists of a set of stadard msbuild 15.0 project files. The standard build procedure follows:

## Prerequisites

- .NET Core 3.0 SDK
- Visual Studio 2017 (Optional)

## Command Line

Build the solution or projects separately using build command:

```shell
dotnet build
```

The build process outputs NuGet packages locally in `\.nugs\` directory. Packages are versioned with `1.0.0` version, `-dev` suffix.

## Visual Studio 2017+

Visual Studio should automatically restore NuGet packages. If this feature is disabled or not working properly, run `dotnet restore` on commandline first.

## Developing with Peachpie from sources

Peachpie platform is distributed as NuGet packages (`Peachpie.*`). After building Peachpie from sources, all the packages are located in `\.nugs\` directory with the suffix `-dev` by default.

In order to use your development packages, after successful build run the helper script `.\update-cache.ps1`. It copies packages from `/.nugs` to your local NuGet packages cache.
   
```shell
PS \build\> .\update-packages.ps1
```

Reference the development packages by your projects, i.e.
```xml
<Project Sdk="Peachpie.NET.Sdk/1.0.0-dev">
```
or
```xml
<PackageReference Include="Peachpie.Library.PDO.MySql" Version="1.0.0-dev" />
```
