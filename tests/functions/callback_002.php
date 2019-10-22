<?php
namespace functions\callback_002;

class A {
    function foo($x) {
        echo __METHOD__;
    }
}

class B {
    function foo($x) {
        echo __METHOD__;
    }
}

$x = new A();

\call_user_func_array([&$x, "foo"], [$x = new B]);

echo " Done.";
