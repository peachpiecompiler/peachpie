<?php
namespace classes\overriding_001;

class A {
    function foo() {}
}

class B extends A {
    function Foo() {} // <-- different casing, compiler synthesizes `override foo()`
}

class C extends B {
    function foo() {} // <-- override the synthesized `foo` override, must not be sealed
}

echo "Done.";
