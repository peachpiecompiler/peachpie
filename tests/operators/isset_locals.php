<?php
namespace operators\isset_locals;

function foo( $arr ) {
	// "extract" introduces locals dynamically,
	// make sure the analysis won't break
	extract( $arr );
	
	if (isset($x)) {
		echo "isset\n";
	}
	else {
		echo "notset\n";
	}		
}

foo( ["x" => 1] );
foo( ["y" => 1] );

echo "Done.";
