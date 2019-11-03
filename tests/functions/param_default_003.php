<?php
namespace functions\param_default_003;

interface I {
    function foo($c = [1,2,3]);
}

class C implements I {
    function foo($c = [1,2,3]) {
        print_r($c);
    }
}

function test(I $x) {
    $x->foo();
}

test(new C);

//
echo "Done.";
