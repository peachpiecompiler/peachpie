<?php
namespace functions\__call_002;

class A
{
  public static function foo() {
    $a = &A::bar();
    return $a;
  }

  public static function __callStatic($method, $arguments)
  {
    return [];
  }
}

print_r(A::foo());
