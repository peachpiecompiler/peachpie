<?php
namespace functions\param_default;

class C {
    const C = 1;
}

function foo($a = C::C) {
    return func_num_args();
}

function test(int $a, $b = ['*'])
{
    echo $a , ", " , count($b), PHP_EOL;
}

$func = "functions\\param_default\\test";
$args = [10];

$func(...$args);

echo foo(), foo(1), foo(1, 2), PHP_EOL;

echo "Done.";
