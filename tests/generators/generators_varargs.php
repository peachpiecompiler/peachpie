<?php
namespace generators\generators_019;

// func_get_args() in generator method
function g($key = null, $operator = null, $value = null)
{
    $a = 1;
    yield 1;

    echo func_num_args();

    $b = 2;
    yield 2;
    
    print_r(func_get_args());
    
    $c = 3;
    yield 3;
}

foreach (g() as $k) echo $k;

echo "\nDone.";