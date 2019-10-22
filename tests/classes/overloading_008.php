<?php
namespace classes\overloading_008;

interface A {
    function foo();
}

interface B extends A {
    function foo();
}

abstract class X implements B {
    // the class should not declare two "foo" methods
}

class Y extends X {
    function foo()
    {
        echo __METHOD__;
    }
}

(new Y)->foo();
