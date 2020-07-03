<?php
namespace functions\return_finally2;

// returning value from "finally"

function f(bool $a) {
	try {
    	return 1;
    }    
    finally {
        if ($a)
    	    return 3;
        echo "?";
    }
}

echo f(true); // 3
