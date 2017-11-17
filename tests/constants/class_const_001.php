<?php

class A {
  const A_CONST = 6;
}

function test($a) {
    echo $a::A_CONST;
}

echo A::A_CONST;

test('A');
test(new A);

echo "\nDone.";
