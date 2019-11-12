<?php
namespace classes\overloading_006_;

class D
{
    public final function foo() {
        return __METHOD__;
    }
}

interface I
{
    public function foo();
}

class A extends D implements I { }

class B extends A implements I { }
