<?php

eval('class A { function foo(){ echo __METHOD__; } }');
eval('class B extends A { function foo(){ echo __METHOD__; } }');
    
(new A)->foo();
(new B)->foo();

echo "Done.";
