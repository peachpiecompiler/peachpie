<?php
namespace arrays\foreach_002;

function test_aliases($arr) {
	
	foreach ($arr as &$value) {
		echo $value, "; ";
	}
	
	foreach ($arr as $key => &$value) {
		echo $key, " => ", $value, "; ";
	}
}

test_aliases([1,2,3,4,5]);
