The solution consists of a set of stadard msbuild 15.0 project files. The standard build procedure follows:

## Prerequisites

- .NET Core 1.1
- .NET Framework 4.6 SDK (Optional, Windows)
- Visual Studio 2017 (Optional)

## Command Line

Either within the repository root `/` or within single project directories `/src/**/`:

1. `dotnet restore`
2. `dotnet build`

This builds project for all targets and creates NuGet packages locally in `/.nugs` directory.

## Visual Studio 2017

Visual Studio should automatically restore NuGet packages. If this feature is disabled or not working properly, run `dotnet restore` on commandline first.

### Known Issues:

- Visual Studio invokes Package Restore infinitely causing high CPU usage. Disable automatic NuGet restore in options.
- Visual Studio incorrectly reports errors in Peachpie.Library.MySql. The project builds ok tho.
- `Peachpie.Library.PDO.IBM` needs to be compiled for either `x86` or `x64`, not `AnyCPU`. Skip its compilation if the library is not needed.

## Developing with Peachpie from sources

Peachpie is distributed as NuGet packages (`Peachpie.*`). After building Peachpie from sources, all the packages are located in `/.nugs` directory with the suffix `-dev` by default. When building a project on top of Peachpie, you have to:

1. In `.msbuildproj` (https://github.com/peachpiecompiler/peachpie/wiki/msbuild) change versions of Peachpie packages to `0.8.0-dev` explicitly.
2. There are two options how to make the restore process to use your development packages
   
   a. create `MyGet.Config` and specify local path to `/.nugs` as the first NuGet packages source.
   
   b. or in `/build` directory, run `.\update-packages.ps1 0.8.0` which copies packages from `/.nugs` to your local NuGet packages cache.
   
   c. or instead of `Peachpie.App` package reference, specify `Peachpie.App.csproj` project reference, in case you contribute to a Peachpie library and not the compiler itself.
   
