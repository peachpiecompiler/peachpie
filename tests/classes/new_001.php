<?php

class Dog {
	function __construct($breed) {		
		echo __METHOD__;
	}
}

class BigDog extends Dog {
	function __construct($breed, $size) {
		echo __METHOD__;
		parent::__construct($breed);
	}
}

new BigDog("wolf", 3.5);

echo "Done.";
