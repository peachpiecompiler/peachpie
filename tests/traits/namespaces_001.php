<?php
namespace traits\namespaces_001;

trait T {
  public function foo() {
    return "foo";
  }
}

class C {
  use T {
    T::foo as bar;
  }
}

echo (new C)->bar();
