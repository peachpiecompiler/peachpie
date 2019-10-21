<?php
namespace traits\trait_006;

// Example #11 Defining Properties
// + static properties and constants

trait PropertiesTrait {
    var $x = 1;
	static $sx = 2;
	// const C = __CLASS__; // PHP does not allow that, we do
}

class PropertiesExample {
    use PropertiesTrait;
}

$example = new PropertiesExample;
echo $example->x, PropertiesExample::$sx; // , PropertiesExample::C;
