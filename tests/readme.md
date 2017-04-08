This folder contains test files that we compile and run using Peachpie. Eventually the output is compared with what returns regular PHP.

## How to run

Tests run automatically on our build servers.

To run the tests locally use `vstest` (Visual Studio 2017+) or `dotnet test` (.NET Core 1.1). The test project is located in `/src/Tests/Peachpie.ScriptTests`.

Example:
1. `cd ../src/Tests/Peachpie.ScriptTests`
2. `dotnet restore`
3. `dotnet test

Optionally, the test runner will try to run `php` command locally. If you have PHP installed, ensure your  `php` command corresponds to PHP version 7 or newer or the version you would like to compare with Peachpie.

## Guidelines

> Note, there are known and intended differences in comparison to the regular PHP. Following recommendations should be preserved.

1. avoid UTF-8 BOM in test files
2. use print_r instead of var_dump
3. avoid displaying warnings and errors, there are known and intended differences in error handling
