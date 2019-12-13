<?php
namespace functions\callback_004;

function foo() {
  echo __FUNCTION__ ."\n";
}

class A {
  public function bar() {
    echo __METHOD__ ."\n";
  }

  public static function baz() {
    echo __METHOD__ ."\n";
  }
}

function test($callable) {
  try {
  	$callable();
  }
  catch (\Throwable $e) {
    echo get_class($e) ."\n";
  }
}

test(__NAMESPACE__ .'\foo');
test(__NAMESPACE__ .'\nonExistent');
test(__NAMESPACE__ .'\A::baz');
test(__NAMESPACE__ .'\A::nonExistent');
test(__NAMESPACE__ .'\nonExistent::baz');
test([__NAMESPACE__ .'\A', 'baz']);
test([new A, 'bar']);
test([__NAMESPACE__ .'\A', 'nonExistent']);
test([__NAMESPACE__ .'\nonExistent', 'baz']);
test([new A, 'nonExistent']);
test([42, 'baz']);
