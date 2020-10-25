<?php
namespace operators\div_001;

function test($x, $y) {
  try {
    echo $x / $y;
  } catch (\Throwable $e) {
    echo \get_class($e);
  }

  echo "\n";
}

test(5, 2);
test(5.0, 2);
test("5.0", 2);
test([5], 2);
test(5, [2]);
