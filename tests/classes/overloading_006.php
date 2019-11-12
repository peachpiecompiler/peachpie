<?php
namespace classes\overloading_006;

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

interface IA extends I { }

interface IB extends I { }

class C extends X implements IA { }

class D extends A implements IB { }

echo (new A)->foo();
echo (new B)->foo();
echo (new C)->foo();
echo (new D)->foo();

echo "Done.";
