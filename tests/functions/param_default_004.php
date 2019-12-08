<?php
namespace functions\param_default_004;

function test_args($p1 = 0, $args = array()) {
	echo '$p1:', $p1, PHP_EOL;
	echo '$args: ';
	print_r($args);
}

call_user_func_array(__NAMESPACE__ . '\test_args', array(2, array('a' => 100, 'b' => 200)));

echo "Done.";
