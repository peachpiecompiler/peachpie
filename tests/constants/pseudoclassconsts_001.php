<?php

namespace A {
	class X {
		function foo() {
			echo static::class;
		}
		static function bar() {
			echo static::class;
		}
	}

	(new X)->foo();
	X::bar();

	echo "Done.";
}
