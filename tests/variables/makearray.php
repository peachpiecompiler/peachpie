<?php
namespace variables\makearray;

/** The test ensures the type analysis correctly handles that Boolean type may change to Array */
function foo($arr) {
	$b = false;
	
	print_r($b);
	
	foreach ($arr as $x)
	{
		$b[] = $x;
	}
	
	print_r($b);
}

foo([]);
foo([1,2,3]);

echo "Done.";
