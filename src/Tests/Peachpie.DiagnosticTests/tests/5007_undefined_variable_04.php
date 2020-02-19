<?php

function never_maybe_defined($foo) {
  if ($foo == 'something') {
    $maybeDefined = 42;
  }

  if (isset($maybeDefined)) {
    echo $maybeDefined;
  } else {
    echo "reachable";
    echo $maybeDefined/* non strict !PHP5007 */;    
  }

  echo $maybeDefined/* non strict !PHP5007 */;

  echo isset($maybeDefined) ? $maybeDefined : $maybeDefined/* non strict !PHP5007 */;

  if (isset($maybeDefined) || isset($neverDefined)) {
    echo $maybeDefined/* non strict !PHP5007 */;
  }

  if (isset($maybeDefined) && isset($neverDefined)) {
    echo $maybeDefined;/*!PHP5011!*/
  }

  if (!isset($maybeDefined)) {
    echo $maybeDefined/* non strict !PHP5007 */;    
  }
}

function always_defined_integer() {
  /*|integer|*/$alwaysDefined = 42;

  if (isset($alwaysDefined)) {
    echo /*|integer|*/$alwaysDefined;
  } else {
    echo "unreachable";/*!PHP5011!*/
    echo /*|integer|*/$alwaysDefined; // since https://github.com/peachpiecompiler/peachpie/issues/675 this variable won't be changed to null
  }

  echo $alwaysDefined;
}

function always_defined_int_null($x) {
  $alwaysDefined = $x ? 42 : null;

  if (isset(/*|integer|null*/$alwaysDefined)) {
    echo /*|integer|*/$alwaysDefined;
  } else {
    echo "reachable";
    echo /*|null|*/$alwaysDefined;
  }

  echo /*|integer|null*/$alwaysDefined;
}

function always_defined_mixed($alwaysDefined) {
  if (isset(/*|mixed|*/$alwaysDefined)) {

    if (isset(/*|mixed|*/$alwaysDefined)) {
      echo /*|mixed|*/$alwaysDefined;
    } else {
      echo "reachable";               // If the type is mixed, we were unable to propagate that it is never null
      echo /*|null|*/$alwaysDefined;
    }

    echo /*|mixed|*/$alwaysDefined;
  }
}

function always_defined_int_null_multiple($x) {
  /*|integer|null|*/$a = $x ? 42 : null;
  /*|integer|null|*/$b = $x ? 42 : null;

  if (isset($a, $b)) {
    echo /*|integer|*/$a;
    echo /*|integer|*/$b;
  } else {
    // We don't know exactly which check failed => can't constraint the type
    echo /*|integer|null|*/$a;
    echo /*|integer|null|*/$b;
  }
}