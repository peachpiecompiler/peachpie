<?php
namespace arrays\arrays_002;

function testarr()
{
	$x[] = 1;
	$x[] = 2;
	$x["k"] = 3;
	$x["l"][] = 4;
	$x[100] = 5;
	$x[] = 6;
	
	echo $x[0], $x[1], $x["k"], $x["l"][0], $x[100], $x[101];
}

testarr();

echo "Done.";
