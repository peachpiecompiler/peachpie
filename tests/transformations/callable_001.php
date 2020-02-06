<?php
namespace transformations\callable_001;

function foo() { echo "foo "; }

class B
{
  public static function bar() { echo "B::bar "; }
}

class A extends B
{
  public static function bar() { echo "A::bar "; }
  public function baz() { echo "baz "; }

  public function test() {
    call_user_func(__NAMESPACE__ ."\\foo");
    call_user_func([__NAMESPACE__ ."\\A", "bar"]);
    call_user_func(["self", "bar"]);
    call_user_func(["parent", "bar"]);
    call_user_func([$this, "baz"]);
    call_user_func(__NAMESPACE__ ."\\foo ");
  }
}

(new A)->test();
