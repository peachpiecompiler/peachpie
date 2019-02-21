<?php

class A {
  public function __construct() {}
}

class A1 extends A {
  public function __construct() {
    parent::__construct();
  }
}

class A2 extends A {
  public function __construct()/*!PHP5026!*/ {
    echo "Missing parent::__construct";
  }
}

class C {
  public $field;
}

class C1 extends C {
  public function __construct() {
    echo "parent::__construct not needed";
  }
}
