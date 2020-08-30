## Testing Peachpie Platform

Peachpie is continuously tested on build servers. Before any contribution it is recommended to run tests locally in order to avoid unnecessary pull requests.

### Prerequisites

1. successfully compiled solution `./Peachpie.sln`
2. local installation of latest `php` which is used by tests

### Running tests on shell

Test projects are maintained in `./src/tests` project directory. Each of the subdirectories contains a test project.

Navigate to `./src/tests/ ... ` individually and run following sequence of commands

1. `dotnet restore`
2. `dotnet test`

Example:

```
D:\Projects\peachpie\src\Tests\Peachpie.DiagnosticTests>dotnet restore
  Restoring packages ...

D:\Projects\peachpie\src\Tests\Peachpie.DiagnosticTests>dotnet test
Build started, please wait...
Build completed.

Test run for D:\Projects\peachpie\src\Tests\Peachpie.DiagnosticTests\bin\Debug\netcoreapp1.0\Peachpie.DiagnosticTests.dll(.NETCoreApp,Version=v1.0)
Microsoft (R) Test Execution Command Line Tool Version 15.0.0.0
Copyright (c) Microsoft Corporation.  All rights reserved.

Starting test execution, please wait...
[xUnit.net 00:00:01.3152602]   Discovering: Peachpie.DiagnosticTests
[xUnit.net 00:00:01.5641356]   Discovered:  Peachpie.DiagnosticTests
[xUnit.net 00:00:01.6565120]   Starting:    Peachpie.DiagnosticTests
[xUnit.net 00:00:03.7684150]   Finished:    Peachpie.DiagnosticTests

Total tests: 9. Passed: 9. Failed: 0. Skipped: 0.
Test Run Successful.
Test execution time: 4,6788 Seconds
```

### Running tests in Visual Studio

After opening `./Peachpie.sln`, the test projects are placed within `Tests` solution folders. Build contained projects and use `Test Explorer` to run and debug test cases.

## Configuration

### PEACHPIE_TEST_PHP

Environment variable `PEACHPIE_TEST_PHP` controls whether *ScriptTests* will compare the results of test cases with actual `php`. 

Setting the variable to `0` will disable the feature.

```
SET PEACHPIE_TEST_PHP=0
```