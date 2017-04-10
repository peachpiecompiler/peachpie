<?php

class DefinedClass {
  const CONSTANT = 42;

  static $staticProperty = 42;

  public $property = 42;

  public function method() {
    return 42;
  }

  public static function staticMethod() {
    return 42;
  }
}

$a = DefinedClass::CONSTANT;
DefinedClass::$staticProperty = DefinedClass::$staticProperty + 1;
$a = DefinedClass::staticMethod();

$goodInstance = new DefinedClass();

if ($goodInstance instanceof DefinedClass) {}
