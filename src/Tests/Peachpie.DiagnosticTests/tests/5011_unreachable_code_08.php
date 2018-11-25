<?php

function unreachable_null_comparison(bool $a, bool $b, bool $c, bool $d) {
  if ($a === null) {
    echo "unreachable";/*!PHP5011!*/
    echo /*|null|*/$a;
  } else {
    echo "reachable";
    echo /*|boolean|*/$a;
  }
  
  if ($b !== null) {
    echo "reachable";
    echo /*|boolean|*/$b;
  } else {
    echo "unreachable";/*!PHP5011!*/
    echo /*|null|*/$b;
  }

  if (/*|boolean|*/$c == null) {
    echo "reachable";
    echo /*|boolean|*/$c;
  } else {
    echo "reachable";
    echo /*|boolean|*/$c;
  }

  if (/*|boolean|*/$d != null) {
    echo "reachable";
    echo /*|boolean|*/$d;
  } else {
    echo "reachable";
    echo /*|boolean|*/$d;
  }

  $null = null;

  if ($null === null) {
    echo "reachable";
    echo /*|null|*/$null;
  } else {
    echo "unreachable";/*!PHP5011!*/
    echo /*|null|*/$null;
  }
  
  if ($null !== null) {
    echo "unreachable";/*!PHP5011!*/
    echo /*|null|*/$null;
  } else {
    echo "reachable";
    echo /*|null|*/$null;
  }
  
  if ($null == null) {
    echo "reachable";
    echo /*|null|*/$null;
  } else {
    echo "unreachable";/*!PHP5011!*/
    echo /*|null|*/$null;
  }
  
  if ($null != null) {
    echo "unreachable";/*!PHP5011!*/
    echo /*|null|*/$null;
  } else {
    echo "reachable";
    echo /*|null|*/$null;
  }
}
