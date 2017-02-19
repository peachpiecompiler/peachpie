This folder contains test files that we run using regular PHP and Peachpie to compare their outputs.

## How to run

Compile `runtests` project in Debug configuration and run `runtests.cmd`. You can also drag&drop a subfolder in this folder onto `runtests.cmd` to run only selected tests.

## Guidelines

1. avoid UTF-8 BOM in test files
2. use print_r instead of var_dump
3. avoid displaying warnings and errors, there are known and intended differences in error handling
