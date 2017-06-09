<?php

function int_check_negation(/*|mixed|*/$x) {
  if (!is_int(/*|mixed|*/$x)) {
    echo /*|mixed|*/$x;
  } else {
    echo /*|integer|*/$x;
  }
}

function null_check_negation(/*|mixed|*/$x) {
  if (!is_null(/*|mixed|*/$x)) {
    echo /*|mixed|*/$x;
  } else {
    echo /*|null|*/$x;
  }
}

function type_separation_negation(/*|mixed|*/$x) {
  /*|boolean|integer|*/$y = $x ? 42 : true;

  if (!is_int($y)) {
    echo /*|boolean|*/$y;
  } else {
    echo /*|integer|*/$y;
  }
}
