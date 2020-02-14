<?php
namespace functions\is_a;

function test($v) {
	if(is_a($v, 'DateTime')) { // type analysis should not expect $v is object!
        // TODO: type analysis should treat $v as of type "\DateTime"
		echo 'DateTime';
	} elseif(is_numeric($v)) {
		echo 'Number';
	} else {
		echo 'unknown';
	}
}

test(1);

echo "Done.";
