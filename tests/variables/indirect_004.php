<?php

function f()
{
	$x = "a";
	$$x = 56;
	echo $$x." ".$a;
}
f();
