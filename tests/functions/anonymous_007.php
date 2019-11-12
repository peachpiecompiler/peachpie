<?php
namespace functions\anonymous_007;

class A {
  private function foo() {
    echo "foo";
  }

  public function bar($method) {
    $fn = function() use ($method) {
      $this->$method();
    };
    $fn();
  }
}

(new A)->bar('foo');
