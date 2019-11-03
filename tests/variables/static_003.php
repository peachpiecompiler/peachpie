<?php
namespace variables\static_003;

function test()
{
    static $c = [];
    if (!isset($c[0]))
    {
        $c[0] = 123;
    }
	
	$c[0] ++;
	
	print_r($c);
}

test();
test();
test();

echo "Done.";