<?php
namespace classes\overloading_006;

interface I
{
    public function foo();
}

abstract class A implements I
{
}

final class B extends A
{
    public function foo() {
        return __METHOD__;
    }
}

echo (new B)->foo();

echo "Done.";
