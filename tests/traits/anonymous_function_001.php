<?php
namespace traits\anonymous_function_001;

trait T {
  private $bar = "bar";

  public function getFn() {
    return function() { return $this->bar; };
  }
}

class C {
  use T;
}

echo (new C)->getFn()();
