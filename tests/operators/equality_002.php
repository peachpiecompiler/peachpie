<?php
namespace operators\equality_002;

function test() {
	$a = [];
    $aRef =& $a;
    echo (int)([] == $a);
    echo (int)([] === $a);
    echo (int)($a == []);
    echo (int)($a === []);
}

test();
