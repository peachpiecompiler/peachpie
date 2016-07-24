<?php

class Dog {
	function __construct($breed) {		
		echo __METHOD__, $breed;
	}
	function Bark(){
		echo "Bark";
	}
}

class BigDog extends Dog {
	function __construct($breed, $size = 0) {
		echo __METHOD__, $breed, $size;
		parent::__construct($breed);
	}
	function Bark(){
		echo "Bark!";
	}
}

function test($name) {
	(new $name("TheDog"))->Bark();
}

test("dog");
test("bigDog");

echo "Done.";
