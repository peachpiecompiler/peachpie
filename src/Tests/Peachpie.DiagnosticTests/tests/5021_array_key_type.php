<?php

class MyClass {}

function test(MyClass $mc, int $i, string $s, $any, $hlp) {
  $is = $hlp ? $i : $s;
  $ismc = $hlp ? $is : $mc;
  $number = strpos("a", "b") + $any; // cannot be array

  return array(
    $i => 'Lorem',
    $s => 'Ipsum',
    $mc/*!PHP5021!*/ => 'Dolor',
    $any => 'Sit',
    $is => 'Amet',
    $ismc/*!PHP5021!*/ => 'Consecteurer',
    $number => "int|double",
  );
}
