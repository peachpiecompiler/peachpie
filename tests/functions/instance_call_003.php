<?php
namespace functions\instance_call_003;

function factory($opt, $throw = false) {
  if ($opt == 1) {
    return new A();
  } else if ($opt == 2) {
    return new B();
  } else {
    return new C();
  }
}

class A {
	public function execute() { echo __METHOD__; }
}
class B {
	public function execute() { echo __METHOD__; }
}
class C {
	public function execute() { echo __METHOD__; }
}

function test() {
  $x = factory(2);

  // This call is bound to wrong type if not reset properly during the analysis
  $x->execute();
}

test();