<?php
namespace transformations\define_001;

define("FOO", "foo ");

echo FOO;

function bar(int $foo) {
  return $foo > 42;
}

if (bar(43)) {
  define("FOO_COND", "bar_cond ");
} else {
  define("FOO_COND", "foo_cond ");
}

echo FOO_COND;

function foo() {
  define("FOO_LOCAL", "foo_local ");
  echo FOO_LOCAL;
}

if (define("FOO_IN_EXPR", "foo_in_expr ") && bar(43)) {
  echo FOO_IN_EXPR;
}
