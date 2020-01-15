<?php
namespace arrays\array_push;

function test() {
    $arr = array();
    $a = 1;
    $c = &$a;
    array_push($arr, $a);

    $a = 666;

    print_r($arr);
}

test();

echo "Done.";
