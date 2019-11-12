<?php
namespace constants\pseudoconsts;

if (true) {
	function foo() { echo __LINE__, __FUNCTION__; }
}
else {
	function foo() { echo __LINE__, __FUNCTION__; }
}

function bar() {
	echo __LINE__, __FUNCTION__;
}

class Test {
	static function xar(){
		echo __LINE__, __FUNCTION__;
	}
}

foo();
bar();
Test::xar();
echo __LINE__, __FUNCTION__;

// TODO: other magic constants

echo "Done.";
