<?php
namespace classes\static_001;

class X {

	static $sfld1;
	
	/** @var int */
	static $sfld2;
	
	/** @appstatic */
	static $sfld3;
	
	static function test1() {
		X::$sfld1 = "Helo";
		X::$sfld2 ++;
		X::$sfld3 = "World";
	}
	
	static function test2() {
		echo X::$sfld1, X::$sfld2, X::$sfld3;
	}
}

X::test1();
X::test2();

echo "Done.";
