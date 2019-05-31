<?php

function parse_url_check1($file) {
  $res = parse_url($file, PHP_URL_SCHEME);
  if (null !== /*|null|string|*/$res) {
    return true;
  }
  return false;
}

function parse_url_check2($file) {
  if (null !== /*|null|string|*/parse_url($file, PHP_URL_SCHEME)) {
    return true;
  }
  return false;
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
