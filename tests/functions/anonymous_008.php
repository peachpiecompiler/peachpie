<?php
namespace functions\anonymous_008;

class A
{
  private $foo;

  private function bar()
  {
    return function() {
      $this->foo = 42;
      yield 42;
    };
  }

  public function test() {
    foreach ($this->bar()() as $v) {
      echo $v;
    }
  }
}

(new A)->test();
