<?php
namespace functions\late_static_002;

class X {
    static function f() {
        echo get_called_class();
    }
    function g() {
        echo get_called_class();
    }
}

class Y extends X {}

X::f();
Y::f();

echo "\n";

(new X)->g();
(new Y)->g();

echo "\nDone.";
