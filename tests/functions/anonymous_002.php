<?php

function f()
{
	$greet = function($name)
	{
		echo "Hello ", $name, "\n";
	};

	$greet('World');
	$greet('Peachpie');
}

f();

echo "Done.";
