<?php
namespace variables\serialize_001;

function f()
{
    $o = (object)["a" => 123, "b" => [1, 2, 3]];
	print_r($o);

	$s = serialize($o);
	print_r($s);
	
	$o = unserialize($s);
	print_r($o);
}

f();

echo "Done";
