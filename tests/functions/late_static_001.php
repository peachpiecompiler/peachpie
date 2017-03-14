<?php

class X {
	function __construct(){ echo __METHOD__, "\n"; }
	static function foo() {
		new static;
	}
}

class Y extends X {
	function __construct(){ echo __METHOD__, "\n"; }
}

X::foo();
Y::foo();

echo "Hello from Peachpie!";
