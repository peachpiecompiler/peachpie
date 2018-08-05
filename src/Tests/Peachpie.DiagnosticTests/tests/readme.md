This folder contains test PHP files annotated with expected diagnostics and types.
They are analysed one by one by Peachpie and the obtained results are compared with the annotations.

> Note that the test files are neither compiled to CIL nor run.

## How to run

The tests run automatically on our build servers.

To run the tests locally, use `vstest` (Visual Studio 2017+) or `dotnet test` (.NET Core 1.1). The test project is located in `/src/Tests/Peachpie.Peachpie.DiagnosticTests`.

Example:
1. `cd ../src/Tests/Peachpie.Peachpie.DiagnosticTests`
2. `dotnet restore`
3. `dotnet test`

## Guidelines

We can test two aspects of analysis: diagnostics and types. Take a look at the following example:

```php
<?php

function foo(/*|mixed|*/$x) {
  /*|boolean|integer|*/$y = $x ? 42 : true;

  if (is_int($y)) {
    echo /*|integer|*/$y;
  } else {
    echo /*|boolean|*/$y;
    return;

    echo "unreachable";/*!PHP5012!*/
  }

  $z = array();
  if (is_null($y)) {
    echo /*|array|*/$z;/*!PHP5012!*/
  }
}
```

As we can see, `$y` can be either of `boolean` or `integer` type. After `is_int($y)` is called, its type is split among the following branches.
`!PHP5012` is the code of the *Unreachable code detected* warning.
It must be raised after `echo "unreachable"`, because it stands right after the return statement, and also in the block conditioned by `is_null($y)`, because `$y` apparently cannot be `null`.
On that `echo` statement, it is shown how to use both annotations at once.
Precise rules of how to use annotations follow:

### Type annotations

* `/*|type1|type2|type3|*/` written right *before* a variable
* Types don't have to be alphabetically ordered, but otherwise annotations must correspond to the output of `TypeRefContext.ToString(TypeRefMask)` enclosed in `/*|`...`|*/`
* They don't need to be written in front of every variable

### Diagnostic annotations

* `/*!PHP1234!*/` written right *after* an expected occurence of the diagnostic with code `PHP1234` (right after the end of its squiggle in an IDE)
* *Only one* diagnostic ending on a given position is supported
* All the expected diagnostics must be written in the file in order not to be reported as missing
