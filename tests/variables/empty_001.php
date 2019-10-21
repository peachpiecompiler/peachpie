<?php
namespace variables\empty_001;

$a = new \stdClass;
$a->p = new \stdClass;
$a->p->p = 123;

echo empty($a) ? 1 : 0;
echo empty($a->p) ? 1 : 0;
echo empty($a->p->p) ? 1 : 0;

class X {
	var $p = 1;
}

function test( $test, X $x ) {
	$a = $test ? $x : false;
	echo empty( $a->p ) ? 1 : 0; // $a is null|X, we have to check nullref here
}

test(false, new X);
test(true, new X);

echo "Done.";
