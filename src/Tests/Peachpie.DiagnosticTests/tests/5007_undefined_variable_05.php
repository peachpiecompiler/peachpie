<?php

function bar($arg) {
  $foo = 42;
  echo $foo;

  unset($foo);
  echo $foo/*!PHP5007!*/;

  $foo = 24;

  if ($arg == 1) {
    unset($foo);
  }

  echo $foo/* non strict PHP5007 */;

  $foo = 24;
  for ($i = 0; $i < 10; $i++) {
    echo $foo/* non strict PHP5007 */;

    unset($foo);

    echo $foo/*!PHP5007!*/;
  }
}
