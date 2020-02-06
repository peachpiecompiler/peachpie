<?php
namespace transformations\skippass_002;

function foo($a, $b) {
  $a = 666;
  echo $b;
}

function test() {
  $a = 42;
  $b =& $a;
  foo($a, $b);
}

test();
