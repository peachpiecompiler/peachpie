<?php

class X
{
	function __toString(){ return __METHOD__; }
}

$b = new X;
$x = "<$b>";
echo $x;
