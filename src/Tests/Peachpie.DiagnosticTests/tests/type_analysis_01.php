<?php

function foo(/*|mixed|*/$x)
{
  /*|boolean|*/$bool = true;
  /*|integer|*/$int = 42;
  /*|double|*/$double = 4.2;
  /*|string|*/$string = "Lorem";
  /*|array|*/$array = array();
  /*|null|*/$null = null;

  /*|boolean[]|*/$bool_array = array(true, false);
  /*|integer[]|*/$int_array = array(42, 43);
  /*|double[]|*/$double_array = array(4.2, 4.3);
  /*|string[]|*/$string_array = array("Lorem", "Ipsum");
  /*|array[]|*/$array_array = array(array(), array());

  // The analysis itself doesn't discard the information, but the string representation does
  /*|array|*/$mixed_array = ($x == 0) ? $int_array : $double_array;

  /*|object|*/$system_object = new System\Object();
  /*|Closure|*/$closure = function($a, $b) { return $a + $b; };
  /*|resource|null|*/$resource = stream_context_create();
  /*|stdClass|*/$stdClass = new stdClass();

  // TODO: Update when made more precise
  /*|object|*/$system_object2 = (object)array('a' => 'b');

  switch (/*|mixed|*/$x) {
    case 0:
      /*|boolean|*/$result = $bool;
      break;
    case 1:
      /*|integer|*/$result = $int;
      break;
    case 2:
      /*|double|*/$result = $double;
      break;
    case 3:
      /*|string|*/$result = $string;
      break;
    case 4:
      /*|array|*/$result = $array;
      break;
    case 6:
      /*|object|*/$result = $system_object;
      break;
    default:
      /*|stdClass|*/$result = $stdClass;
      break;
  }

  return /*|array|boolean|double|integer|stdClass|string|object|*/$result;
}

/*|array|boolean|double|integer|stdClass|string|object|*/$res = foo(42);

function bar(bool .../*|boolean[]|*/$x) {
  return /*|boolean[]|*/$x;
}

/*|boolean[]|*/$res = bar();

function baz(int /*|integer|*/$x) {
  echo /*|integer|*/$x;

  // TODO: Enable annotations also for this statement (it's not in the CFG -> unable to be annotated)
  global $k;

  return /*|mixed|*/$k;
}

/*|mixed|*/$res = baz(42);

/*|integer|*/$i = 5;
// global variables may be changed from outside, so the type is always mixed
echo /*|mixed|*/$i;

function callable_check(
    callable /*|array|Closure|object|string|*/$c,
    callable &/*|array|Closure|object|string|*/$cr,
    ?callable /*|array|Closure|null|object|string|*/$cn1,
    callable /*|array|Closure|null|object|string|*/$cn2 = null)
{}
