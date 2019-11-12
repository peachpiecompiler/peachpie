<?php
namespace classes\anonymous_001;

function f()
{
	$x = new class
	{
		function __construct() { echo "ctor\n"; }
		function foo(){ return __FUNCTION__; }
	};
	echo $x->foo();
}

f();

echo "Done.";
