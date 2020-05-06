<?php
namespace functions\param_null_001;

function test(\DateTime $v = null) {
	if(null === $v) {   // https://github.com/peachpiecompiler/peachpie/issues/355 the condition was optimized out
		echo 'null';
	} else {
		echo 'something';
    }
}

test();

echo "Done.";
