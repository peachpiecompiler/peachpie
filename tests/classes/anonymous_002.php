<?php
namespace classes\anonymous_002;

class C {
    private function foo() { echo "foo"; }
    private static function bar() { echo "bar"; }

    public static function getFn() {
        return function () {
            // following private methods have to be called dynamically
            // since scope can change:
            (new C)->foo();
            C::bar();
        };
    }
}

//
$f = C::getFn();
$f();

echo "\nDone.";