<?php
namespace arrays\array_combine;

function test($a, $b, $fn) {
  print_r(array_combine($a, $b));
  print_r($fn($a, $b));

  $c = array_combine($a, array_pad(array(), count($a), array("foo" => "bar", "bar" => "foo")));
  print_r($c);
  // Update a specific subkey, should not mutate the others
  $c["B"]["foo"] = "notbar";
  print_r($c);
}

test(['a' => 'A', 'b' => 'B', 'c' => 'C', 'd' => 'D'], ['w' => 'W', 'x' => 'X', 'y' => 'Y', 'z' => 'Z'], 'array_combine');
