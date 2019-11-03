<?php
namespace operators\comparison_001;

class A
{
  function __toString() { return "foo"; }
}

class B { }

function test() {
  $a = new A;
  echo (int)("foo" === $a);
  echo (int)("foo" == $a);
  echo (int)("foo" > $a);
  echo (int)("foo" < $a);
  echo (int)("foo" >= $a);
  echo (int)("foo" <= $a);
  echo (int)("bar" === $a);
  echo (int)("bar" == $a);
  echo (int)("bar" > $a);
  echo (int)("bar" < $a);
  echo (int)("bar" >= $a);
  echo (int)("bar" <= $a);
  echo (int)($a === "foo");
  echo (int)($a == "foo");
  echo (int)($a > "foo");
  echo (int)($a < "foo");
  echo (int)($a >= "foo");
  echo (int)($a <= "foo");
  echo (int)($a === "bar");
  echo (int)($a == "bar");
  echo (int)($a > "bar");
  echo (int)($a < "bar");
  echo (int)($a >= "bar");
  echo (int)($a <= "bar");

  $b = new B;
  echo (int)("foo" === $b);
  echo (int)("foo" == $b);
  echo (int)("foo" > $b);
  echo (int)("foo" < $b);
  echo (int)("foo" >= $b);
  echo (int)("foo" <= $b);
  echo (int)($b === "foo");
  echo (int)($b == "foo");
  echo (int)($b > "foo");
  echo (int)($b < "foo");
  echo (int)($b >= "foo");
  echo (int)($b <= "foo");
}

test();
