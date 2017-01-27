<?php

function f() {
	preg_match('/(foo)(bar)(baz)/', 'foobarbaz', $matches, PREG_OFFSET_CAPTURE);
	print_r($matches);
}

f();