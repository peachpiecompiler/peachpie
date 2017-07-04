<?php

function foo()
{
    $array = [
        [1, 2],
        [3, 4],
    ];

    // unpacking foreach variable to list()

    foreach ($array as list($a, $b)) {
        echo "A: $a; B: $b\n";
    }
}

foo();
