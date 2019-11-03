<?php
namespace classes\ctor_overloading;

class X {
	function __construct($a, $b = []) {
		echo __METHOD__;
		print_r($a);
		print_r($b);
	}
}

class Y extends X {
	// inherit ctor
}

class Z extends Y {
	function __construct($a, $b = 123, array $c = []) {
		echo __METHOD__;
		print_r($a);
		print_r($b);
		print_r($c);
	}
}

new X(1);
new X(2, [1, 2]);
new X(3, "Hello");

new Y(4);
new Y(5, [4, 5]);
new Y(6, "World");

new Z(7);
new Z(8, "Dog");
new Z(9, "Cat", [7, 8]);
