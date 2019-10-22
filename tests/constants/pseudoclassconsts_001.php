<?php

namespace constants\pseudoclassconsts_001 {
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
