<?php
namespace classes\static_003;

class Test
{
  public static function staticMethod() {
    echo "foo";
    static::anotherStaticMethod();
  }

  public static function anotherStaticMethod() {
    echo "bar";
  }
}

function test(Test $a) {
  $a->staticMethod();
}

test(new Test());
echo "\n";

// TODO: Enable when the type for late static binding is obtained dynamically
//class SubTest extends Test
//{
//  public static function anotherStaticMethod() {
//    echo "rab";
//  }
//}
//
//test(new SubTest());
