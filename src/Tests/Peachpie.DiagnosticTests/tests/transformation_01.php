<?php

function bar()/*{version:0}*/ {
  echo "bar";
}

function foo()/*{version:1}*/ {
  bar();

  if (function_exists('print_r')) {
    return 0;
  } else {
    return "yes";/*!PHP5011!*/
  }
}

function baz(int $arg)/*{version:1}*/ {
  $a = foo();
  echo /*|integer|*/$a;

  if ($arg) {
    echo /*|integer|*/$a;
  }

  return $arg + 1;
}

