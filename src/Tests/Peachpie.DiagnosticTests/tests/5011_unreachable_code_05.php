<?php

function unreachable_bool_true() {
  if (true) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }
}

function unreachable_bool_false() {
  if (false) {
    echo "unreachable";/*!PHP5011!*/
  } else {
    echo "reachable";
  }
}

function unreachable_int_zero() {
  if (0) {
    echo "unreachable";/*!PHP5011!*/
  } else {
    echo "reachable";
  }
}

function unreachable_int_nonzero() {
  if (-42) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }
}

function unreachable_float_zero() {
  if (0.0) {
    echo "unreachable";/*!PHP5011!*/
  } else {
    echo "reachable";
  }
}

function unreachable_float_nonzero() {
  if (0.01) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }
}

function reachable() {
  if (true) {
    echo "reachable";
  }

  echo "reachable";
}
