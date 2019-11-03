<?php
namespace functions\static_call_002;

class A {
	static function foo() {
		echo __CLASS__, PHP_EOL;
	}

	function test() {
		$this->foo();
	}
}

class B extends A {
	static function foo() {
		echo __CLASS__, PHP_EOL;
	}
}

(new A)->test();
(new B)->test();

echo "Done.";
