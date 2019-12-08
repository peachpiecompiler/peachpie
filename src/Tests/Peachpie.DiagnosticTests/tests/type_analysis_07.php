<?php

function parse_url_check1($file) {
  /*|array|boolean|*/$res = parse_url($file);
  if (null !== $res) {
    return true;
  }
  return false;/*!PHP5011!*/
}

function parse_url_check2($file) {
  if (null !== /*|array|boolean|*/parse_url($file)) {
    return true;
  }
  return false; // condition above is not being evaluated yet, so this won't be marked as unreachable
}

function not_null_attr_check() {
  $ext = new ReflectionExtension("Reflection");
  /*|string|*/$name = $ext->getName();
  if (null === $name) {
    echo "unreachable";/*!PHP5011!*/
  }
}

function cast_to_false_check() {
  $cls = new ReflectionClass("ReflectionClass");
  /*|boolean|string|*/$comment = $cls->getDocComment();
  if (null === $comment) {
    echo "unreachable";/*!PHP5011!*/
  }
}
