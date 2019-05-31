<?php

class A {
  public function __toString() {
    return "Foo";
  }
}

echo (string)new A;
