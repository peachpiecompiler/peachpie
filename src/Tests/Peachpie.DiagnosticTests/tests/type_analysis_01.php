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

  /*|System\Object|*/$system_object = new System\Object();
  /*|Closure|*/$closure = function($a, $b) { return $a + $b; };
  /*|resource|*/$resource = stream_context_create();
  /*|stdClass|*/$stdClass = new stdClass();
  
  // TODO: Update when made more precise
  /*|System\Object|*/$system_object2 = (object)array('a' => 'b');

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
      /*|System\Object|*/$result = $system_object;
      break;
    case 7:
      /*|resource|*/$result = $resource;
      break;
    default:
      /*|stdClass|*/$result = $stdClass;
      break;
  }

  echo /*|array|boolean|double|integer|resource|stdClass|string|System\Object|*/$result;
}