<?php

function test($a)/*{version:2}*/ {
  if ($a) {
    $b = 42;
  } else if (!function_exists('print_r')) {
    $b = "foo";/*!PHP5011!*/
  } else {
    $b = 0.1;
  }

  if (is_string($b)) {
    return "bar";/*!PHP5011!*/
  } else {
    return 24;
  }
}

echo /*|integer|*/test(true);
