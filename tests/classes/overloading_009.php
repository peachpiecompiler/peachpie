<?php
namespace classes\overloading_009;

interface A { function foo(); }

interface B extends A { public function foo(); }

abstract class W implements A {}

abstract class X extends W implements B {}

class Y extends X {
  public function foo() {
    echo __METHOD__;
  }
}

(new Y)->foo();
