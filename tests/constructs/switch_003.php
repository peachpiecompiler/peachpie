<?php
namespace constructs\switch_003;

function test($a)
{
	switch ($a)	// $a is mixed
	{
		case 1: return "1";
		case 2: return "2";
		case 3: return "3";
		case 4: return "4";
		case 5: return "5";
		default: return "default";
	}
}

echo test(123);

echo "Done.";
