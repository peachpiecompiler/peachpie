<?php

function test($x)
{
	print_r($x);
	var_export($x);
	//var_dump($x);
}

$a = 123;

test([null, 1, "key" => 1.2, "text", "multiline
text", true, false, &$a, ["a", "b", "c"], []]);

echo "Done.";
