<?php
namespace traits\static_001;

trait T {
  public static $bar = "bar";
  
  public static function foo() {
    echo "foo";
  }
}

T::foo();
echo T::$bar;
