<?php

function bar($a)/*{version:1}*/ {
  if (function_exists('print_r')) {
    return "bla";
  } else {
    return 42;/*!PHP5011!*/
  }
}

function baz($a)/*{version:1}*/ {
  return bar($a);
}

function foo($a)/*{version:2}*/ {
  if ($a) {
    return bar($a);
  } else {
    return baz($a);
  }
}

function main()/*{version:0}*/ {
  foo(42);
}