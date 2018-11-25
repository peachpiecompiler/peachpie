<?php

function never_maybe_defined($foo)/*{version:1}*/ {
  if ($foo == 'something') {
    $maybeDefined = 42;
  }

  if (isset($maybeDefined)) {
    echo $maybeDefined;
  } else {
    echo "reachable";
    echo $maybeDefined/* non strict !PHP5007 */;
  }
}

function test($a)/*{version:1}*/ {
  if (!function_exists('print_r')) {
    return "foo";/*!PHP5011!*/
  } else {
    return 0.1;
  }
}
