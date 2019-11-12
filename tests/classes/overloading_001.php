<?php
namespace classes\overloading_001;

class Dog {
	function Whof(){ echo "whof"; }
}

class BigDog {
	function Whof(){ echo "WHOF!"; }
}

function test($dog) {
	$dog->Whof();
}

function test2() {
	$dog = new BigDog;
	$dog->Whof();
}

test(new Dog);
test(new BigDog);
test2();

echo "Done.";
