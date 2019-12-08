<?php
namespace functions\runtimechain_001;

class X
{
    function foo($a, &$b) {
        echo $a, PHP_EOL;
        $b = 666;
    }
}

function test($x)   // $x does not have a type specified
{
    @$x->foo( 1, $b->prop->prop );
    print_r($b);

    @$x->foo( 2, $c[]);
    print_r($c);

    @$x->foo( 3, $d[1]->prop );
    print_r($d);

    @$x->foo( 4, $e->arr["index"]->prop );
    print_r($e);

    $arr = [];
    $x->foo( 5, $arr["index"] );
    print_r($arr);

}

test(new X);

echo "Done.";
