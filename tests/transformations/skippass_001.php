<?php
namespace transformations\skippass_001;

class A {
  var $p; // ref

  // Modification in method call

  private function setP() {
    $this->p = 666;
  }

  private function foo1($p) {
    $this->setP();
    echo $p;
  }

  public function test1() {
    $p = 42;
    $this->p =& $p;
    $this->foo1($p);
  }

  // Modification in __toString

  public function __toString() {
    $this->p = 666;
    return "";
  }

  private function foo2($p) {
    echo $this;
    echo $p;
  }

  public function test2() {
    $p = 42;
    $this->p =& $p;
    $this->foo2($p);
  }

  private function foo3($p) {
    $s = $this . "";
    echo $p;
  }

  public function test3() {
    $p = 42;
    $this->p =& $p;
    $this->foo3($p);
  }

  private function foo4($p) {
    $s = (string)$this;
    echo $p;
  }

  public function test4() {
    $p = 42;
    $this->p =& $p;
    $this->foo4($p);
  }

  // Explicit modification through a field

  private function foo5($p) {
    $this->p = 666;
    echo $p;
  }

  public function test5() {
    $p = 42;
    $this->p =& $p;
    $this->foo5($p);
  }

  // Modification in __clone

  public function __clone() {
    $this->p = 666;
  }

  private function foo6($p) {
    $that = clone $this;
    echo $p;
  }

  public function test6() {
    $p = 42;
    $this->p =& $p;
    $this->foo6($p);
  }
}

(new A)->test1();
echo "\n";
(new A)->test2();
echo "\n";
(new A)->test3();
echo "\n";
(new A)->test4();
echo "\n";
(new A)->test5();
echo "\n";
(new A)->test6();
