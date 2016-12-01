##Readme

*ConsoleApplication1* is a simple console project written in PHP running on .NET Core.

## Prerequisites

- .NET Core 1.0 or later
- Either sources of Peachpie compiler so `dotnet restore` can locate the project files, or update `project.json` with specific Peachpie dependencies version.

## Build & Run

1. `dotnet restore` - The command locates specified dependencies including compiler, runtime and referenced libraries and downloads them.
2. `dotnet run` - The command actually builds the project to .NET assembly and executes its entry point.
