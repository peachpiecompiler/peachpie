<?php

function foo($a, $b = null, $c = null) {}

function test1($x/*{skipPass:1}*/, array $a/*{skipPass:1}*/, string $s/*{skipPass:1}*/) {
  // The main target scenario - simply passing the arguments to another routine
  foo($x, $a, $s);
}

function test2($x/*{skipPass:1}*/, $y/*{skipPass:0}*/) {
  foo($x);
  foo($y);  // The first call might have modified $y
}

function test3($x/*{skipPass:0}*/, string $s/*{skipPass:1}*/) {
  echo $s;
  echo $s;  // The echo of string can't cause any side effect

  echo $x;
  echo $x;  // The first echo might have caused the call to __string with any code
}

function test4($x/*{skipPass:0}*/) {
  for ($i = 0; $i < 5; $i++) {
  	foo($x);
  }
}
