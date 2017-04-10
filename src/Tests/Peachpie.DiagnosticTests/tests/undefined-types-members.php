<?php
// Examples of diagnostics of undefined types and members
//
// Currently, only undefined types (in various contexts, as seen below) are reported.
// Dynamic nature of PHP complicates the analysis of undefined properties and methods; therefore,
// it's not implemented at the moment.

// Definitions

function definedFunction($a, $b) {
  return $a + $b;
}

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

class DefinedException extends Exception {}

// Defined entities - diagnostics won't appear

$a = definedFunction(5, 4);
$a = DefinedClass::CONSTANT;
DefinedClass::$staticProperty = DefinedClass::$staticProperty + 1;
$a = DefinedClass::staticMethod();

$goodInstance = new DefinedClass();
$goodInstance->property = $goodInstance->property + 1;
$goodInstance->method();

if ($goodInstance instanceof DefinedClass) {}

try {
} catch (DefinedException $exception) {
}

// Undefined types - diagnostics will appear for Undefined* (and nothing else)

$b = undefinedFunction/*!PHP0051!*/(5, 4);
$b = UndefinedClass/*!PHP0053!*/::Constant;
$b = UndefinedClass/*!PHP0053!*/::$staticProperty;
$b = UndefinedClass/*!PHP0053!*/::staticMethod();
$b = UndefinedClass/*!PHP0053!*/::undefinedStaticMethod();

$badInstance = new UndefinedClass/*!PHP0053!*/();
$badInstance->property = $badInstance->property + 1;
$badInstance->method();

if ($badInstance instanceof UndefinedClass/*!PHP0053!*/) {}

try {
} catch (UndefinedException/*!PHP0053!*/ $exception) {
}

// Unimplemented - diagnostics might appear in the future

$b = DefinedClass::UndefinedStaticMethod();

$goodInstance->UndefinedMethod();
$goodInstance->undefinedProperty = $goodInstance->undefinedProperty + 1;

$className = "OtherUndefinedClass";
$d = new $className();
