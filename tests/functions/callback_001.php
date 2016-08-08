<?php

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
	call("X::bar");
	call(["X", "bar"]);
	call([$x, "foo"]);
}

test();

echo "Done.";
