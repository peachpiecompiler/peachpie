<?php
namespace classes\overloading_011;

class A {
    function foo(string $a) {
        print_r(func_num_args()); // causes "foo" to have additional params parameter at end
    }
}
class B extends A {
    function foo() {
        print_r(func_get_args()); // causes "foo" to have additional params parameter at end
    }
}

function test(A $a) {
    $a->foo(1, 2, 3); // calling foo(string, params PhpValue[]);
}

test(new B);

echo PHP_EOL, "Done.";
