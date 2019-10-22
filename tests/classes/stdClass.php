<?php
namespace classes\stdClass;

function test() {
	$x = new \stdClass;
	$x->a = 1;
	$x->b = 2;
	echo $x->a, $x->b;
	
	$y = null;
	@$y->a = 3;
	@$y->b = 4;
	echo $x->a, $x->b;
}

test();

echo "Done.";
