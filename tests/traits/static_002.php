<?php
namespace traits\static_002;

trait T {
  private static $bar = "right";

  public static function bar() {
    return self::$bar;
  }
}

class C {
  public static $bar = "wrong";

  public static function foo() {
    return T::bar();
  }
}

echo C::foo();
echo "\n";
echo T::bar();
