<?php
namespace transformations\ord_002;

function ord(string $s): int {
  return 4200;
}

function test(string $s, int $i) {
  echo ord($s[$i]);
}

test("foo", 1);