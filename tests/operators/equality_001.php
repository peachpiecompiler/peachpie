<?php
namespace operators\equality_001;

function b($value) {
	echo ($value == null) ? 0 : 1;
}

function test() {
	b(null);
	b(true);
	b(false);
	b([]);
	b(new \stdClass);
	b("hello");
	b("0");
	b([0]);
}

test();