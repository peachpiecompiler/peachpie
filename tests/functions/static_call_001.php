<?php
namespace functions\static_call_001;

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

test('functions\\static_call_001\\A');
test(new A);

echo "\nDone.";
