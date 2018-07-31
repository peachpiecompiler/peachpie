<?php

function foo($x) {
  if ($x) {
    return true;
  } else {
    return;
  }
}

/*|boolean|void|*/$res = foo(42);
