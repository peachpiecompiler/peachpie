<?php
namespace operators\assign_003;

function test1($i1, $i2, $a1, $a2, $s1, $s2) {
  $i1 = $i1 + 1;
  $i1 = $i1 - 1;
  $i1 = $i1 + $i2;
  $i1 = $i1 - $i2;
  $i1 = $i1 * $i2;
  $i1 = $i1 / $i2;
  $i1 = $i1 & $i2;
  $i1 = $i1 ^ $i2;
  $i1 = $i1 | $i2;
  $i1 = $i1 << $i2;
  $i1 = $i1 >> $i2;
  $i1 = $i1 % $i2;
  $i1 = $i1 ** $i2;

  $a1 = $a1 + $a2;

  $s1 = $s1 . $s2;

  echo $i1 ."\n";
  print_r($a1);
  echo $s1 ."\n";
}

function test2(int $i1, int $i2, array $a1, array $a2, string $s1, string $s2) {
  $i1 = $i1 + 1;
  $i1 = $i1 - 1;
  $i1 = $i1 + $i2;
  $i1 = $i1 - $i2;
  $i1 = $i1 * $i2;
  $i1 = $i1 / $i2;
  $i1 = $i1 & $i2;
  $i1 = $i1 ^ $i2;
  $i1 = $i1 | $i2;
  $i1 = $i1 << $i2;
  $i1 = $i1 >> $i2;
  $i1 = $i1 % $i2;
  $i1 = $i1 ** $i2;

  $a1 = $a1 + $a2;

  $s1 = $s1 . $s2;

  echo $i1 ."\n";
  print_r($a1);
  echo $s1 ."\n";
}

test1(5, 2, [1, 2], [3, 4], "foo", "bar");
test2(5, 2, [1, 2], [3, 4], "foo", "bar");
