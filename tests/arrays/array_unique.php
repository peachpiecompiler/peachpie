<?php
namespace arrays\array_unique;

function test($a, $fn) {
    print_r($a);
    print_r(array_unique($a));
    print_r($fn($a));

    $b = null;
    $res = array_unique($b);
    echo gettype($res) . "=" . $res . PHP_EOL;
}

test(["foo", "bar", "foo", 0, 1, 2, 0], 'array_unique');
