<?php
namespace variables\hinting_001;

class X {
	
}

function test(X $x) {
	
}

test(new X);

echo "Done.";
