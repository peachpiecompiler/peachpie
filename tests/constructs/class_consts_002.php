<?php
namespace constructs\class_consts_002;

class Test
{
    const string C = 'hello';
}

$name = "C";

echo Test::C, PHP_EOL;
echo Test::{$name}, PHP_EOL;
echo 'Done.';
