<?php
namespace functions\fdiv;
use const NAN;
use const INF;

function test( float $a, float $b, float $expected ) {
    if( function_exists("fdiv") ) {
        $x = fdiv($a, $b);
        if ($x == $expected || (is_nan($x) && is_nan($expected)))
            echo "ok";
        else
            echo "($a / $b) failed";
    }
    else {
        echo "ok";
    }
    echo PHP_EOL;
}

test(10, 3, 10/3);
test(10., 3., 10/3);
test(-10., 2.5, -10/2.5);
test(10., -2.5, 10/-2.5);

test(10., 0., INF);
test(10., -0., -INF);
test(-10., 0., -INF);
test(-10., -0., INF);

test(INF, 0., INF);
test(INF, -0., -INF);
test(-INF, 0., -INF);
test(-INF, -0., INF);

test(0., 0., NAN);
test(0., -0., NAN);
test(-0., 0., NAN);
test(-0., -0., NAN);

test(INF, INF, NAN);
test(INF, -INF, NAN);
test(-INF, INF, NAN);
test(-INF, -INF, NAN);

test(0., INF, 0);
test(0., -INF, -0.0);
test(-0., INF, -0.0);
test(-0., -INF, 0);

test(NAN, NAN, NAN);
test(INF, NAN, NAN);
test(0., NAN, NAN);
test(NAN, INF, NAN);
test(NAN, 0., NAN);

//
echo "Done.";