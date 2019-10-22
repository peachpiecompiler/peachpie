<?php
namespace reflection\ReflectionMethod_001;

interface I {
	function f();
}

class A {
    public function f() {}
}

class B extends A {
    public function f() {}
}

class C extends B implements I {
    public function f() {}
}

class D extends C {

}

function test() {
	$m = new \ReflectionMethod(__NAMESPACE__ . "\\D", "f");

	echo $m->class, PHP_EOL;					// C
	echo $m->getPrototype()->class, PHP_EOL;	// I

	$m = new \ReflectionMethod(__NAMESPACE__ . "\\B", "f");
	echo $m->getPrototype()->class, PHP_EOL;	// A

	try {
		echo $m->getPrototype()->getPrototype()->class, PHP_EOL;	// ReflectionException
	}
	catch (\Throwable $e) {
		echo "no prototype", PHP_EOL;
	}
}

test();

echo "Done.";
