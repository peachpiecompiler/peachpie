<?php

class A extends ArrayObject {
  function foo($val) {
    $this[$val] = $val;
    echo $this[$val];
  }
}

(new A([]))->foo(42);
