<?php
namespace classes\static_007;

// See https://www.php.net/manual/en/language.oop5.late-static-bindings.php#example-243
// Bug: https://github.com/peachpiecompiler/peachpie/issues/703

trait T {
	public static function init() {
		self::forwarding(); // <-- has to forward late static type "B"
	}

	public static function forwarding() {
		echo static::$name; // <-- static should be "B"
	}
}

abstract class A {
	use T;
}

class B extends A {
	public static $name = 'customers';

	public static function init() {
		parent::init();
	}
}

B::init();

echo "Done.";
