<?php
namespace functions\param_default_002;

trait T
{
	public function foo($a = ['*'], $b = self::class ) {
		print_r($a);
		print_r($b);
		echo PHP_EOL;
	}
}

trait U
{
	use T;
}

class X
{
	use T;

	function bar($a = ['*'], $b = []) {
		print_r($a);
		print_r($b);
	}
}

// test 1
$x = new X;
$x->foo();
$x->bar();

// test 2
(new X)->foo();

// test 3
(new X)->bar();

// test 4
foreach ((new \ReflectionMethod(X::class , 'foo'))->getParameters() as $p) {
	print_r($p->getDefaultValue());
}

//
echo "Done.";
