<?php

namespace T252;

class X { function foo(&$a) { $a = 666; } }

//
function test1($x, $a) {
    $x->foo($a);
    echo $a, ',';
}

test1(new X, 1);

//
function test2($x, string $a) {
    $x->foo($a);
    echo $a, ',';
}

test2(new X, "two");

//
function test3($x, int $a) {
    $x->foo($a);
    echo $a, ',';
}

test3(new X, 3);

//
function test4($a) {
    (new X)->foo($a);
    echo $a, ',';
}

test4(new X);

//
$x = new X;
$a = 555;
$x->foo($a);
echo $a, ',';

//
function test6($x, $a) {
    $x->foo($a[0]);
    echo $a[0], ',';
}

//test6(new X, ['six']); // TODO https://github.com/peachpiecompiler/peachpie/issues/252

//
function test7($x, array $a) {
    $x->foo($a[0]);
    echo $a[0], ',';
}

//test7(new X, ['seven']); // TODO https://github.com/peachpiecompiler/peachpie/issues/252

//
function test8(X $x, $a) {
    $x->foo($a);
    echo $a, ',';
}

test8(new X, 8);

//
function test9($x) {
    $a = "nine";
    $x->foo($a);
    echo $a, ',';
}

test9(new X);

//
echo 'Done.';
