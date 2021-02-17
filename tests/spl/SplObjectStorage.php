<?php
namespace spl\SplObjectStorage;

use SplObjectStorage;
use stdClass;

// $x->foo( $arr[$key] ) where $arr is Spl SplObjectStorage, $key is object`, $key must not be converted to IntStringKey

class X {
    function foo($p) {
        echo $p;
    }
}

function test($x, $arr) {
    $key1 = new stdClass;
    $key2 = new stdClass;

    $arr[$key1] = 1;
    $arr[$key2] = 2;

    $x->foo($arr[$key1]); // $key must not be converted to IntStringKey
    $x->foo($arr[$key2]);
}

$x = new X;
$arr = new SplObjectStorage(new stdClass);

test($x, $arr);
test($x, $arr);

echo PHP_EOL, "Done.";
