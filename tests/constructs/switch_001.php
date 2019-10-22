<?php
namespace constructs\switch_001;

/** @param int $a */
function test($a)
{
	switch ($a)
	{
		case 1:
			echo "1";
			break;
		case 2:
			echo "2";
			//break;
		case 3:
			echo "3";
			break;
		case 4:
			echo "4";
			//break;
		case 123:
			echo "123";
			//break;
		case 456:
			echo "456";
			break;
		default:
			echo "default";
			break;
	}
}

test(123);