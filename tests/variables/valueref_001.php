<?php
namespace variables\valueref_001;

function test()
{
	$x = "Hi";
	$a = [1, 2, &$x];
	$a[2] = "Helo";
	echo $x;
}

test();

echo "Done.";
