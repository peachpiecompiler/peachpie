<?php
namespace scripting\eval_001;

eval('namespace scripting\eval_001; class A { function foo(){ echo __METHOD__; } }');
eval('namespace scripting\eval_001; class B extends A { function foo(){ echo __METHOD__; } }');

(new A)->foo();
(new B)->foo();

echo "Done.";
