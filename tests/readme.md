
This folder contains test files that we compile and run using Peachpie. Eventually the output is compared with what regular PHP returns.

## How to run

The tests run automatically on our build servers.

To run the tests locally, use `vstest` (Visual Studio 2017+) or `dotnet test` (.NET Core 1.1). The test project is located in `/src/Tests/Peachpie.ScriptTests`.

Example:
1. `cd ../src/Tests/Peachpie.ScriptTests`
2. `dotnet restore`
3. `dotnet test`

## Comparing with PHP

Optionally, the test runner will try to run the `php` command locally. If you have PHP installed, ensure your  `php` command corresponds to *PHP version 7* or newer or the version you would like to compare with Peachpie.

In case an older version of PHP is installed on your system, some PHP7 specific tests will *fail*.

## Guidelines

> Note that there are known and intended differences in comparison to regular PHP. The following recommendations should be observed:

1. avoid UTF-8 BOM in test files
2. use `print_r` instead of `var_dump`
3. avoid displaying warnings and errors; there are known and intended differences in error handling
4. put the code into a unique namespace so it will get compiled nicer

## Skipped tests

Tests whose name starts with either `skip_*` or `skip(*)_*` are skipped. If possible please use the second variant and specify a reason why the test is skipped inside the parentheses.
- E.g.: `skip(late_static_binding_forwarding_not_supported)_static_004.php`
> `*` is a wildcard for arbitrary string.
