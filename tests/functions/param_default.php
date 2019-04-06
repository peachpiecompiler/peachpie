<?php

function test(int $a, $b = ['*'])
{
    echo $a , ", " , count($b);
}

$func = "test";
$args = [10];

$func(...$args);

echo "\nDone.";
