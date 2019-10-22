<?php
namespace classes\static_004;

class A
{
    function bar()
    {
        echo __METHOD__, "\n";
    }
}

class B extends A
{
    function bar()
    {
        echo __METHOD__, "\n";
    }

    function foo($name)
    {
        static::bar();
        $name::bar();
        parent::bar();
        A::bar();
    }
}

$x = new B;

$x->foo(__NAMESPACE__ . "\\B");
$x->foo(__NAMESPACE__ . "\\A");

echo "Done.";
