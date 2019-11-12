<?php
namespace functions\unpacking_003_byref;

function test($val1, $val2, &...$refs) {
    foreach ($refs as &$ref) ++$ref;
}

function foo1()
{
	$array = [1, 2, 3, 4, 5];
	test(...$array);
	print_r($array); // [1, 2, 4, 5, 6]
}

foo1();

echo "Done.";