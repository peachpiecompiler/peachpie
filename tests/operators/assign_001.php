<?php

function test(int $x) {
  $x |= 1;
  $x ^= 255;
  $x &= 5;
  echo $x;
}

test(42);
