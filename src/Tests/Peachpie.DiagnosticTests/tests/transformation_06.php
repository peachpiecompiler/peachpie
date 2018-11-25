<?php

function test1($a)/*{version:1}*/ {
  if ($a)
    return -1;

	switch (true) {
		default:
			break;
	}

	if (false) {
    return 1;/*!PHP5011!*/
	}

  return 0;
}

function test2($a)/*{version:1}*/ {
  foreach ($a as $i) {
    echo $i;
  }

  if (!function_exists('print_r')) {
    return "foo";/*!PHP5011!*/
  } else {
    return 0.1;
  }
}
