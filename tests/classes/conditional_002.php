<?php
namespace classes\conditional_002;

$a = true;

if ($a)
{
    class A
    {
        function foo() { echo __METHOD__; }
    }
	
    interface I
    {
    
    }
}
else
{
    class A
    {
        function bar() { echo __METHOD__; }
    }
	
    interface I
    {
    
    }
}

class B extends A implements I
{
    function foo() { echo __METHOD__; }
}

(new B)->foo();

echo "Done.";
