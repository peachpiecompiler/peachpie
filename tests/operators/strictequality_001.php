<?php
namespace operators\strictequality_001;

function b($b) {
	echo $b ? 1 : 0;
}

function test() {
	$b = 1.0 < 2.0;

	b($b);
	b($b == 1);
	b($b === 1);
	b($b === true);
	b(true === $b);
	b(false === $b);
	b(1 === $b);
	b($b === 0.0);
	b(1.0 === $b);
	b("1" === $b);
	b($b === "1");
}

test();