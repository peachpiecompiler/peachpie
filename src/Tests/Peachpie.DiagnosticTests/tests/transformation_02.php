<?php

function bar($a)/*{version:1}*/ {
  if (function_exists('print_r')) {
    return "bla";
  } else {
    return 42;/*!PHP5011!*/
  }
}

function baz($a)/*{version:1}*/ {
  return /*|string|*/bar($a);
}

function foo($a)/*{version:1}*/ {
  if ($a) {
    return /*|string|*/bar($a);
  } else {
    return /*|string|*/baz($a);
  }
}

function main()/*{version:1}*/ {
  foo(42);
}