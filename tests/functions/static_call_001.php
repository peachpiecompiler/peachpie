<?php

class A
{
	static function foo(){ echo __METHOD__; }

	static function bar()
	{
		self::foo();
	}
}

function test($a) {
    $a::foo();
    $a::bar();
}

test('A');
test(new A);

echo "\nDone.";
