<?php

function test($x/*{skipPass:1}*/, array $a/*{skipPass:1}*/, string $s/*{skipPass:1}*/) {
  echo $x;
  print_r($a);
  echo $s;
}

function modify(&$x) {
  $x[0] = 'b';
}

function test2($x/*{skipPass:0}*/, array $a/*{skipPass:0}*/, string $s/*{skipPass:0}*/) {
  modify($x);
  modify($a);
  modify($s);

  echo $x;
  print_r($a);
  echo $s;
}

function test3($x/*{skipPass:0}*/, array $a/*{skipPass:0}*/) {
  array_pop($x);
  array_pop($a);

  echo $x;
  print_r($a);
}

test("foo", ['a'], "bar");
test2("foo", ['a'], "bar");
