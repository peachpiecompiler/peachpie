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
