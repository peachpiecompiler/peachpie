<?php
namespace transformations\callable_001;

function foo() { echo "foo "; }

class A
{
  public static function bar() { echo "bar "; }
  public function baz() { echo "baz "; }

  public function test(array $arr) {
    array_map(__NAMESPACE__ ."\\foo", $arr);
    array_map([__NAMESPACE__ ."\\A", "bar"], $arr);
    array_map([$this, "baz"], $arr);

    // TODO: Re-enable when warning mode for invalid callbacks works
    //array_map(__NAMESPACE__ ."\\foo ", $arr);
  }
}

(new A)->test([0]);
