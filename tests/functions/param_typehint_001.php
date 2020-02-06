<?php
namespace functions\param_typehint_001;

class A {}

class MyArrayAccess implements \ArrayAccess
{
  function offsetExists($offset) {}
  function offsetGet($offset) {}
  function offsetSet($offset, $value) {}
  function offsetUnset($offset) {}
}

function check_array(array $a) {}

function test_array($x) {
  try {
    check_array($x);
    echo "OK";
    echo is_array($x);
    echo $x[0];
  }
  catch (\Throwable $err) {
    echo get_class($err);
  }

  echo " ";
}

function check_object(object $o) {}

function test_object($x) {
  try {
    check_object($x);
    echo "OK";
    echo is_object($x);
  }
  catch (\Throwable $err) {
    echo get_class($err);
  }

  echo " ";
}

function test() {
  test_array(true);
  test_array(42);
  test_array(4.2);
  test_array("foo");
  test_array([42]);
  test_array(new A);
  test_array(new MyArrayAccess);
  test_array(function() {});
  test_array(null);
  echo "\n";

  test_object(true);
  test_object(42);
  test_object(4.2);
  test_object("foo");
  test_object([42]);
  test_object(new A);
  test_object(new MyArrayAccess);
  test_object(function() {});
  test_object(null);
}

test();
