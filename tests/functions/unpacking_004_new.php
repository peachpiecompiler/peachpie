<?php
namespace functions\unpacking_004_new;

class X
{
	function __construct($a, $b, $c)
	{
		print_r($a);
		print_r($b);
		print_r($c);
	}
}

function foo1()
{
	$array = [1, 2, 3, 4, 5];
	new X(...$array);
}

foo1();

echo "Done.";