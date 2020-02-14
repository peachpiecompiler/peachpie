<?php
namespace variables\valueref_005;

function foo(array $arr) {
	return $ret = &$arr['x']; // issue alias was not dereferenced: https://github.com/peachpiecompiler/peachpie/issues/647
}

function test() {
    global $a;
    $a = foo([]);   // RValue expected to be dereferenced
}

test();

echo "Done.";
