<?php
namespace transformations\callable_001;

function foo() { echo "foo "; }

class A
{
  public static function bar() { echo "bar "; }
  public function baz() { echo "baz "; }

  public function test(array $arr) {
    array_map("foo", $arr);
    array_map(["A", "bar"], $arr);
    array_map([$this, "baz"], $arr);

    array_map("foo ", $arr);
  }
}

(new A)->test([0]);
