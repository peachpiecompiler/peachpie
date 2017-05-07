<?php

function unreachable_simple($x) {

  return;

  echo "unreachable";/*!PHP3012!*/
}

function unreachable_if($x) {

  return;

  if ($x == 0/*!PHP3012!*/) {
    return;
  } else {
    return;
  }
}

function unreachable_switch($x) {

  return;

  switch ($x/*!PHP3012!*/ ) {
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

  echo "unreachable";/*!PHP3012!*/
}
