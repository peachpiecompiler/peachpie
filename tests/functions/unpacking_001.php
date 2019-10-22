<?php
namespace functions\unpacking_001;

function test(...$args) { print_r($args); }

function foo1()
{
	test(1, 2, 3);                         // [1, 2, 3]
	test(...[1, 2, 3]);                    // [1, 2, 3]
	test(...new \ArrayIterator([1, 2, 3])); // [1, 2, 3]
}

function foo2()
{
	$args1 = [1, 2, 3];
	$args2 = [4, 5, 6];
	test(...$args1, ...$args2); // [1, 2, 3, 4, 5, 6]
	test(1, 2, 3, ...$args2);   // [1, 2, 3, 4, 5, 6]
}

foo1();
foo2();

echo "Done.";