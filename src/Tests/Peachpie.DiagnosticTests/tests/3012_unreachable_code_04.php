<?php

function unreachable_after_if($x) {
  if ($x == 0) {
    return;
  } else {
    return;
  }

  echo "unreachable";/*!PHP3012!*/
}

function reachable_after_if($x) {
  if ($x == 0) {
    return;
  } else {
    echo "reachable";
  }

  echo "reachable";
}

function reachable_referenced($x) {
  $y = 42;

  if ($x) {
    $ref =& $y;
    $ref = null;
  }

  if (is_int($y)) {
    echo "reachable";
    echo /*|integer|*/$y;
  } else {
    echo "reachable";
    echo /*|integer|*/$y;
  }
}
