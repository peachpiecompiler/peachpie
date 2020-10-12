<?php
namespace arrays\array_fill;

function test($fn) {
  print_r(array_fill(0, 2, "foo"));
  print_r($fn(1, 3, "bar"));

  $p = array_fill(0, 2, array("foo" => "bar", "bar" => "foo"));
  print_r($p);
  // Update a specific subkey, should not mutate the others
  $p[1]["foo"] = "notbar";
  print_r($p);
}

test('array_fill');
