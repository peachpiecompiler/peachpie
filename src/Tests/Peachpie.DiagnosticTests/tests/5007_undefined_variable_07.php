<?php

function bar($foo) {
  if ($foo == 'something') {
    $maybeDefined = array(42 => 'answer');
  }

  if (isset($maybeDefined[42])) {
    echo $maybeDefined;
    echo $maybeDefined[42];
    echo $maybeDefined[1000];
  } else {
    echo $maybeDefined;/* non strict PHP5007 */
  }

  echo $maybeDefined/* non strict PHP5007 */[42];
}
