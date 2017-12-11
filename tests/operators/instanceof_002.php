<?php
class A {
  public function test($x) {
    if ($x instanceof $this) {
      echo "good";
    }
  }
}
(new A)->test(new A);

echo "\nDone.";
