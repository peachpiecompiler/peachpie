<?php
namespace variables\unset_004;

function test($t) {
	$v = 'example';
	
	if($t) {
		unset($v);
	}

	if(isset($v)) {
		echo 'fail', PHP_EOL;
	}
}

test(20);

echo "Done.";
