<?php

class A {
  function foo($a, $b = "") { echo __METHOD__, $a, $b, "\n"; }
}
class B extends A {
  function foo($a) { echo __METHOD__, $a, "\n"; }
}
class C extends B {
  function foo($a) { echo __METHOD__, $a, "\n"; }
}
class D extends C {
  function foo($a, $b) { echo __METHOD__, $a, $b, "\n"; }
}

(new A)->foo(1);
(new A)->foo(1,2);
(new B)->foo(1);
(new C)->foo(1);
(new D)->foo(1,2);

echo "Done.";
