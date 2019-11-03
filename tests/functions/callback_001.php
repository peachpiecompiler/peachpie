<?php
namespace functions\callback_001;

class X
{
	function foo(){ echo __METHOD__; }
	static function bar(){ echo __METHOD__; }
	function __invoke(){ echo __METHOD__; }
}

function call($x) { $x(); }

function test()
{
	$x = new X;
	call($x);
	call(__NAMESPACE__ . "\\X::bar");
	call([__NAMESPACE__ . "\\X", "bar"]);
	call([$x, "foo"]);
}

test();

echo "Done.";
