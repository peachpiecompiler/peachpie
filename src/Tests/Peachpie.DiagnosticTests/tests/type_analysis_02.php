<?php

function int_check(/*|mixed|*/$x) {
  if (is_int(/*|mixed|*/$x) || is_integer(/*|mixed|*/$x) || is_long(/*|mixed|*/$x)) {
    echo /*|integer|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }
}

function bool_check(/*|mixed|*/$x) {
  if (is_bool(/*|mixed|*/$x)) {
    echo /*|boolean|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }
}

function double_check(/*|mixed|*/$x) {
  if (is_double(/*|mixed|*/$x) || is_double(/*|mixed|*/$x) || is_real(/*|mixed|*/$x)) {
    echo /*|double|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }
}

function string_check(/*|mixed|*/$x) {
  if (is_string(/*|mixed|*/$x)) {
    echo /*|string|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }
}

function resource_check(/*|mixed|*/$x) {
  if (is_resource(/*|mixed|*/$x)) {
    echo /*|resource|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }
}

function null_check(/*|mixed|*/$x) {
  if (is_null(/*|mixed|*/$x)) {
    echo /*|null|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }
}

function array_check(/*|mixed|*/$x) {
  if (is_array(/*|mixed|*/$x)) {
    echo /*|mixed|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }

  /*|boolean[]|integer|*/$y = ($x == 0) ? array(true, false) : 42;
  if (is_array(/*|boolean[]|integer|*/$y)) {
    echo /*|boolean[]|*/$y;
  } else {
    echo /*|integer|*/$y;
  }

  /*|array|integer|*/$z = ($x == 0) ? array() : 42;
  if (is_array(/*|array|integer|*/$z)) {
    echo /*|array|*/$z;
  } else {
    echo /*|integer|*/$z;
  }
}

function object_check(/*|mixed|*/$x) {
  class MyClass {}

  switch ($x) {
    case 0:
      /*|integer|*/$y = 42;
      break;
    case 1:
      /*|string|*/$y = "Lorem";
      break;
    case 2:
      /*|Closure|*/$y = function($a, $b) { return $a + $b; };
      break;
    case 3:
      /*|MyClass|*/$y = new MyClass();
      break;
    case 3:
      /*|null|*/$y = null;
      break;
    default:
      /*|object|*/$y = new System\Object();
      break;
  }

  if (is_object(/*|Closure|integer|MyClass|null|string|object|*/$y)) {
    echo /*|Closure|MyClass|object|*/$y;
  } else {
    echo /*|integer|null|string|*/$y;
  }
}

function numeric_check(/*|mixed|*/$x) {
  if (is_numeric(/*|mixed|*/$x)) {
    echo /*|mixed|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }

  if (is_string(/*|mixed|*/$x) || is_int(/*|mixed|*/$x) || is_float(/*|mixed|*/$x) || is_null(/*|mixed|*/$x)) {
    if (is_numeric(/*|double|integer|null|string|*/$x)) {
      echo /*|double|integer|string|*/$x;
    } else {
      echo /*|null|string|*/$x;
    }
  }
}

function callable_check(/*|mixed|*/$x) {
  if (is_callable(/*|mixed|*/$x)) {
    echo /*|mixed|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }

  switch (/*|mixed|*/$x) {
    case 0:
      /*|integer|*/$y = 42;
      break;
    case 1:
      /*|string|*/$y = "Lorem";
      break;
    case 2:
      /*|Closure|*/$y = function($a, $b) { return $a + $b; };
      break;
    case 3:
      /*|array|*/$y = array();
      break;
    default:
      /*|object|*/$y = new System\Object();
      break;
  }

  if (is_callable(/*|array|Closure|integer|string|object|*/$y)) {
    echo /*|array|Closure|string|object|*/$y;
  } else {
    echo /*|array|integer|string|object|*/$y;
  }
}