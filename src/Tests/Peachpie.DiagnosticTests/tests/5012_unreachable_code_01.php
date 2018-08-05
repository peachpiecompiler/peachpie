<?php

function unreachable_simple($x) {

  return;

  echo "unreachable";/*!PHP5012!*/
}

function unreachable_if($x) {

  return;

  if ($x == 0/*!PHP5012!*/) {
    return;
  } else {
    return;
  }
}

function unreachable_switch($x) {

  return;

  switch ($x/*!PHP5012!*/ ) {
    case 0:
      break;
  }
}

function unreachable_after_switch($x) {
  switch ($x) {
    case 0:
      return;
    default:
      return;
  }

  echo "unreachable";/*!PHP5012!*/
}
