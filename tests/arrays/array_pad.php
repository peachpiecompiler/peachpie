<?php
namespace arrays\array_pad;

function test($a, $fn) {
  print_r(array_pad(array(), 3, 5));
  print_r($fn($a, 3, "foo"));

  $p = array_pad(array(), 3, array("foo" => "bar", "bar" => "foo"));
  print_r($p);
  // Update a specific subkey, should not mutate the others
  $p[1]["foo"] = "notbar";
  print_r($p);
}

test(["test" => "test"], 'array_pad');
