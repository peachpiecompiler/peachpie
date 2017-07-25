<?php

class X {
    public final function foo() {
        return __METHOD__;
    }
}

interface I {
    public function foo();
}

class A extends X implements I { }

class B extends A implements I { }

echo (new B)->foo();

echo "Done.";
