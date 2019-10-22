<?php
namespace constants\class_const_001;

class A {
  const A_CONST = 6;
}

function test($a) {
    echo $a::A_CONST;
}

echo A::A_CONST;

test(__NAMESPACE__ . "\\A");
test(new A);

echo "\nDone.";
