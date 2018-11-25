<?php

function unreachable_bool($x) {
  if (is_int($x)) {
    if (is_bool($x)) {
      echo "unreachable";/*!PHP5011!*/
      echo /*|boolean|*/$x;
    }

    echo /*|integer|*/$x;
  }

  $y = true;
  if (is_bool($y)) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }
}

function unreachable_int($x) {
  if (is_bool($x)) {
    if (is_int($x)) {
      echo "unreachable";/*!PHP5011!*/
      echo /*|integer|*/$x;
    }
  }

  $y = 0;
  if (is_int($y)) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }
}

function unreachable_float($x) {
  if (is_bool($x)) {
    if (is_float($x)) {
      echo "unreachable";/*!PHP5011!*/
      echo /*|double|*/$x;
    }
  }

  $y = 0.5;
  if (is_float($y)) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }
}

function unreachable_string($x) {
  if (is_bool($x)) {
    if (is_string($x)) {
      echo "unreachable";/*!PHP5011!*/
      echo /*|string|*/$x;
    }
  }

  $y = "Lorem";
  if (is_string($y)) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }
}

function unreachable_null($x) {
  if (is_bool($x)) {
    if (is_null($x)) {
      echo "unreachable";/*!PHP5011!*/
      echo /*|null|*/$x;
    } else {
      echo "reachable";
    }
  }

  $y = null;
  if (is_null($y)) {
    echo "reachable";
  } else {
    echo "unreachable";/*!PHP5011!*/
  }
}
