<?php
namespace transformations\skipcopy_001;

function return_direct($a) {
    return $a;
}

function test_direct() {
    $a1 = [0 => 42];
    $a2 = return_direct($a1);

    $a2[0] = 666;
    echo $a1[0];
}

function return_indirect($a) {
    $b = $a;
    $b[1] = 1;
    return $b;
}

function test_indirect() {
    $a1 = [0 => 42];
    $a2 = return_indirect($a1);

    $a2[0] = 666;
    echo $a1[0];
}

test_direct();
test_indirect();
