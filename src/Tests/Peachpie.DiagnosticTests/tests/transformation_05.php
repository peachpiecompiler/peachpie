<?php

function test(int $a)/*{version:1}*/ {
  while ($a > 0) {
    echo "a";
    $a--;
  }

  if (!function_exists('print_r')) {
    return "foo";/*!PHP5011!*/
  } else {
    return 0.1;
  }
}
