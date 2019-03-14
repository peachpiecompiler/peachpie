<?php

class MyClass {}

function test(MyClass $mc, int $i, string $s, $any, $hlp) {
  $is = $hlp ? $i : $s;
  $ismc = $hlp ? $is : $mc;

  return array(
    $i => 'Lorem',
    $s => 'Ipsum',
    $mc/*!PHP5021!*/ => 'Dolor',
    $any => 'Sit',
    $is => 'Amet',
    $ismc/*!PHP5021!*/ => 'Consecteurer'
  );
}
