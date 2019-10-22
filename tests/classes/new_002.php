<?php
namespace classes\new_002;

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

test(__NAMESPACE__ . "\\dog");
test(__NAMESPACE__ . "\\bigDog");

echo "Done.";
