<?php

function test() {
	$b = 1.0 < 2.0;

	echo ($b) ? 1 : 0;
	echo ($b == 1) ? 1 : 0;
	echo ($b === 1) ? 1 : 0;
	echo ($b === true) ? 1 : 0;
}

test();