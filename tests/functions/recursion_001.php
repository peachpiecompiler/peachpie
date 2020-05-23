<?php
namespace functions\recursion_001;

// tests the analysis won't get stuck on recursion
// https://github.com/peachpiecompiler/peachpie/issues/754

function r(int $a) {
    return $a ?? r($a + 1);
}

function q() {
    $a = r(10);
    
    if (defined("X"))
    {
        echo $a;
    }
}

q();

echo "Done.";
