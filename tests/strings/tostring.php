<?php
namespace strings\tostring;

interface MyInterface {
    public function __toString();
}

function foo(MyInterface $a) {
	return (string)$a;
}
