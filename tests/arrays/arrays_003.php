<?php
namespace arrays\arrays_003;

function testarr(){
	$arr = ["a" => 1, "b" => 2, 3 => 3];
	$arr[] = 4;
	$arr[] = [["Hello"]];
	
	echo $arr[5][0][0];	//  Hello
}

testarr();

echo "\nDone.";
