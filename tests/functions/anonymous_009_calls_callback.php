<?php
namespace functions\anonymous_009;

class X {
    
    protected static function foo() {
        echo __METHOD__;
    }

    static function test() {
        $f = function() {
            $callback = [__CLASS__, "foo"];
            call_user_func($callback);  // $callback must be resolved within current class context
        };
        $f();
    }
}

X::test();

//
echo 'Done.';
