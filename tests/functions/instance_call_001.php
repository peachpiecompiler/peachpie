<?php
namespace functions\instance_call_001;

class A
{
	function foo(){ echo __METHOD__; }

	function bar()
	{
		self::foo();
	}
}

class B extends A
{
	function foo(){ echo __METHOD__; }
}

(new A)->bar();
(new B)->bar();

@A::bar();
@B::bar();

echo "Done.";
