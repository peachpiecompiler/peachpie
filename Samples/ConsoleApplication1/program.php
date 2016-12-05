<?php

function test()
{
	$arr = [1, 2];
	$arr[] = &['x'];
	$arr[] = 3;
	$arr[] = &$arr[2];

	var_dump($arr);
	var_export($arr);
	print_r($arr);
}

test();