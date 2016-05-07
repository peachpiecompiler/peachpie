<?php

function test()
{
	$x = "Hi";
	$a = [1, 2, &$x];
	$a[2] = "Helo";
	echo $x;
}

test();

echo "Done.";
