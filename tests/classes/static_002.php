<?php

class X {

	const A = 123;
	
	static $A = 456;
	
	const B = "Hello";
	
	/** @var string */
	static $B = "World!";
}

echo X::A, X::$A;

echo X::B, " ", X::$B;

echo "Done.";
