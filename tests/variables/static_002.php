<?php
namespace variables\static_002;

function foo() {
	static $b = true;
	static $x = null;
	static $i = 123;

	if (isset($b)) echo 'b';
	if (isset($x)) echo 'x';
	if (isset($i)) echo 'i';

	$b  = $i = $x = null;

	$x = (object)[1, 2, 3];
}

foo();
foo();

echo "Done.";
