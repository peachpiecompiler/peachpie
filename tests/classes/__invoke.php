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

class A {
    function __invoke() : int // invoke with type hint
    {
        return 3;
    }
}

echo (new X)(1, 2);
echo (new Y)(1, 2);
echo (new A)();
