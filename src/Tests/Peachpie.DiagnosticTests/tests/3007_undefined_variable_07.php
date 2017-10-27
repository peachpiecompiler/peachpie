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
    echo $maybeDefined/*!PHP3007!*/;    
  }

  echo $maybeDefined/*!PHP3007!*/[42];
}
