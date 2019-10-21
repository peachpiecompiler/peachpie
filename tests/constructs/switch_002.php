<?php
namespace constructs\switch_002;

function test($a)
{
	switch ($a)	// $a is mixed
	{
		case 1: return "1";
		case 2: return "2";
		case 123: return "123";
		case false: return "false";
		case "hello": return "hello";
		case "Hello": return "Hello";
		case 1.0: return "1.0";
		case 1.2: return 1.2;
		case true: return "true";
		default: return "default";
	}
}

echo test(123);
echo test(1.2);
echo test(false);
echo test(true);
echo test("Hello");

echo "Done.";
