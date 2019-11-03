<?php
namespace variables\static_001;

function test()
{
	static $x;
	static $y = 123;
	static $loaded = false;
	
	if (!$loaded) {
		$loaded = true;
		echo "First";
	}
	
	$x ++;
	$y ++;
	
	echo $x, $y;
}

test();
test();
test();
test();
test();
test();

echo "Done.";
