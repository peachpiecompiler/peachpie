<?php
namespace functions\is_numeric;

function test($x) {
	echo var_export($x, true), \is_numeric($x) ? " is numeric" : " is not numeric", PHP_EOL;
}

$ref = "0xcaffe";

$tests = array(
    "42",
    1337,
    0x539,
    02471,
    0b10100111001,
    //1337e0,
    "0x539",
    "02471",
    "0b10100111001",
    "1337e0",
    "not numeric",
    array(),
    9.1,
    null,
    &$ref,
);

foreach ($tests as $x) {
    test($x);
}

echo "Done.";
