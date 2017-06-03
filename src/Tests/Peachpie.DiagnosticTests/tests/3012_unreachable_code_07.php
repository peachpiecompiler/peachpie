<?php

function unreachable_negated_false() {
  if (!false) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP3012!*/
  }
}

function unreachable_negated_zero() {
  if (!0) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP3012!*/
  }
}

function unreachable_negated_nonzero() {
  if (!42) {
    echo "unreachable";/*!PHP3012!*/
  } else {
    echo "reachable";
  }
}

function unreachable_negated_int_check($x) {
  $y = 0;
  if (!is_int($y)) {
    echo "unreachable";/*!PHP3012!*/
  } else {
    echo "reachable";
  }
}
