<?php
namespace variables\indirect_004;

function f()
{
	$x = "a";
	$$x = 56;
	echo $$x." ".$a;
}
f();
