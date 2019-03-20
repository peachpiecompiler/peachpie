<?php

// Test if PhpArray iterator is not changing the values to references (issue #345)

function test() {
	$t = 3;
	$a = array('v1' => 1, 'v2' => 2, 'v3' => &$t);
	
	foreach($a as &$v) {
		$v = $v + 1;
	}
	
	$b = $a;
	$a['v1'] = 10; // $b shouldn't change
	
	print_r($a);
	print_r($b);
}

test();