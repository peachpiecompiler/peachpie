<?php
namespace arrays\arrays_001;

function testarr()
{
	$arr = [1, 2, "Hello"];
	echo $arr[0], $arr[1], $arr[2];
	$arr = [["World"]];
	echo $arr[0][0];
}

testarr();

echo "Done.";
