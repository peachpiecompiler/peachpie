<?php

class A {
  public static function foo() {}
}

class B extends A {
  public static function __callStatic($fn, $args) {}
}

function test() {
  A::foo();
  A::missing/*!PHP5009!*/();

  B::foo();
  B::missing();
}
