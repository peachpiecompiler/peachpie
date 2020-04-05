<?php
namespace functions\indirect_001;

function foo($a, $b = null, $c = null) {
    print_r(func_get_args());
}

function test($fn) {
    $fn("bar");
}

test(__NAMESPACE__ . "\\foo");

//
echo 'Done.';
