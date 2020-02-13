<?php
namespace spl\RecursiveArrayIterator_001;

class A {
  public $foo = 1;
  public $bar = 2;
  public $baz = 3;

  public function __toString() {
    return "A";
  }
}

function test($flags) {
  $it = new \RecursiveArrayIterator([1, 2, [3, 4, new A, 8], 9, new A], $flags);
  $tit = new \RecursiveIteratorIterator($it);

  foreach( $tit as $key => $value ){
    for ($i = 0; $i < $tit->getDepth(); $i++) {
      echo "  ";
    }
    echo $value . "\n";
  }

  echo "\n";
}

test(0);
test(\RecursiveArrayIterator::CHILD_ARRAYS_ONLY);
