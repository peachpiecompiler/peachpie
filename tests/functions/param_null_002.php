<?php
namespace functions\param_null_002;

class C
{
  public static function foo(string $n = null) {
    echo "foo";
  }

  public static function bar() {
    static::foo(null);
  }
}

C::bar();
