<?php
namespace functions\unpacking_002;

function test($arg1, $arg2, $arg3 = null)
{
    print_r($arg1);
	print_r($arg2);
	print_r($arg3);
}

test(...[1, 2]);       // 1, 2
test(...[1, 2, 3]);    // 1, 2, 3
test(...[1, 2, 3, 4]); // 1, 2, 3 (remaining arg is not captured by the function declaration)

echo "Done.";