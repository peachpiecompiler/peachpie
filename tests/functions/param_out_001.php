<?php
namespace functions\param_out_001;

function foo()
{
	$result = str_replace( 'l', 'L', "Hello", $count );
	echo $result, ' (', $count, 'x)';
}

foo();
