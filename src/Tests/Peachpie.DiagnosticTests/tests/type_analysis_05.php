<?php

function mixed_null_comparison($x) {
  if (/*|mixed|*/$x == null) {
    echo /*|mixed|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }
  
  if (/*|mixed|*/$x != null) {
    echo /*|mixed|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }
  
  if (/*|mixed|*/$x === null) {
    echo /*|null|*/$x;
  } else {
    echo /*|mixed|*/$x;
  }

  if (/*|mixed|*/$x !== null) {
    echo /*|mixed|*/$x;
  } else {
    echo /*|null|*/$x;
  }
}

function finite_types_null_comparison($x) {
  $y = $x ? 42 : null;

  if (/*|integer|null|*/$y == null) {
    echo /*|integer|null|*/$y;
  } else {
    echo /*|integer|*/$y;
  }
  
  if (/*|integer|null|*/$y != null) {
    echo /*|integer|*/$y;
  } else {
    echo /*|integer|null|*/$y;
  }
  
  if (/*|integer|null|*/$y === null) {
    echo /*|null|*/$y;
  } else {
    echo /*|integer|*/$y;
  }

  if (/*|integer|null|*/$y !== null) {
    echo /*|integer|*/$y;
  } else {
    echo /*|null|*/$y;
  }
}

function parameter_switched_null_comparison($x) {
  $y = $x ? 42 : null;

  if (null == /*|integer|null|*/$y) {
    echo /*|integer|null|*/$y;
  } else {
    echo /*|integer|*/$y;
  }
  
  if (null != /*|integer|null|*/$y) {
    echo /*|integer|*/$y;
  } else {
    echo /*|integer|null|*/$y;
  }
  
  if (null === /*|integer|null|*/$y) {
    echo /*|null|*/$y;
  } else {
    echo /*|integer|*/$y;
  }

  if (null !== /*|integer|null|*/$y) {
    echo /*|integer|*/$y;
  } else {
    echo /*|null|*/$y;
  }
}
