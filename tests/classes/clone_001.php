<?php
namespace classes\clone_001;

class A {
  var $p;

  public function __construct(&$var) {
    $this->p =& $var;
  }
}

function test() {
  $var = 42;
  $a = new A($var);
  $b = clone $a;
  $var = 24;
  echo $b->p;
}

test();
