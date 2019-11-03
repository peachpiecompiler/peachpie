<?php
namespace classes\__tostring_002;

class A {
  public function __toString() {
    return "Foo";
  }
}

echo (string)new A;
