<?php
namespace arrays\foreach_001;

function test1() {
	$arr = [1,2,3,4,5];
	
	foreach ($arr as $value) {
		echo $value, "; ";
	}
	
	foreach ($arr as $key => $value) {
		echo $key, " => ", $value, "; ";
	}
}

test1();
