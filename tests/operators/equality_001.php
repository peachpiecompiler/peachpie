<?php

function b($value) {
	echo ($value == null) ? 0 : 1;
}

function test($value) {
	b(null);
	b(true);
	b(false);
	b([]);
	b(new stdClass);
	b("hello");
	b("0");
	b([0]);
}
