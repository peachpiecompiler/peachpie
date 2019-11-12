<?php
namespace functions\anonymous_001;

function f()
{
	$u1 = "u1";
	$u2 = "u2";
	$x = function ($a) use ($u1, &$u2) {

		echo $a, $u1, $u2;
		$u2 = "U2";
	};
	$x("a");
	echo $u2;
}

f();

echo "Done.";
