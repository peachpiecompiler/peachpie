<?php
namespace classes\new_001;

class Dog {
	function __construct($breed) {		
		echo __METHOD__, $breed;
	}
}

class BigDog extends Dog {
	function __construct($breed, $size) {
		echo __METHOD__, $breed, $size;
		parent::__construct($breed);
	}
}

new BigDog("wolf", 3.5);

echo "Done.";
