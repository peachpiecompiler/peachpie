<?php
namespace classes\overloading_002;

class A
{
	function foo() : int { return 123; } 
}

class B extends A
{
	function foo($a = 999) : int { return $a; }
}

function test() {
	$a = new A;
	echo $a->foo();
	
	$a = new B;
	echo $a->foo(555);
	echo $a->foo();
}

test();

echo "Done.";
