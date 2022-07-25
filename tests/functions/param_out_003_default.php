<?php

namespace T253;

class X {
    function foo(&$matches = null) {
        $matches = 123;
    }
}

function test($p) {
    $p->foo($matches);
    echo $matches;
}

test( new X );

//
echo 'Done.';
