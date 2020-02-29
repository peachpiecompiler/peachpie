<?php

namespace functions_param_default_006;

class X {
    function test(&$p = null) {
        $p = true;
    }
}

function newx(): object { return new X; }

$x = newx();
$x->test(); // callsite

//
echo 'Done.';
