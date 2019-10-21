<?php
namespace constructs\for_001;

/** @param int $n */
function test($n) {
	for ($i = 0; $i < $n; $i++) {
		echo $i;
	}
}

test(10);