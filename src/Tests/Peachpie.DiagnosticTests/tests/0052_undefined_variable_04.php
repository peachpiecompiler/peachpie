<?php

function bar($foo) {
  if ($foo == 'something') {
    $maybeDefined = 42;
  }

  if (isset($maybeDefined)) {
    echo $maybeDefined;
  } else {
    echo $maybeDefined/*!PHP3007!*/;    
  }

  echo $maybeDefined/*!PHP3007!*/;

  echo isset($maybeDefined) ? $maybeDefined : $maybeDefined/*!PHP3007!*/;

  if (isset($maybeDefined) || isset($neverDefined)) {
    echo $maybeDefined/*!PHP3007!*/;
  }

  if (isset($maybeDefined) && isset($neverDefined)) {
    echo $maybeDefined;
  }

  if (!isset($maybeDefined)) {
    echo $maybeDefined/*!PHP3007!*/;    
  }
}

function baz() {
  $alwaysDefined = 42;

  if (isset($alwaysDefined)) {
    echo $alwaysDefined;
  } else {
    echo $alwaysDefined;    
  }

  echo $alwaysDefined;

  if (!isset($alwaysDefined)) {
    // It may be defined but potentially NULL
    echo $alwaysDefined;
  }
}
