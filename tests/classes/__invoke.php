<?php
namespace classes\__invoke;

class X {
	function __invoke($a, $b)
	{
		return $a . $b;
	}
}

class Y extends X {
	function __invoke($a, $b)
	{
		return $b . $a;
	}
}

echo (new X)(1, 2);
echo (new Y)(1, 2);
