<?php
namespace arrays\ensure_001;

function testarr(){
	$patternses = [];	
	foreach ( [1, 2, 3] as $type ) {
		$patternses[][$type] = "a";
		$patternses[][$type] = "b";
		$patternses[][$type] = "c";
	}
	
	foreach ( $patternses as $patterns ) {
		foreach ( $patterns as $type => $char ) {
			echo "[][$type] = '$char'\n";
		}
	}
}

testarr();

echo "\nDone.";
