<?php
namespace arrays\array_pop_001;


function test($a) {
  array_pop($a);
  $a[] = "last";
  print_r($a);
}

test(["foo"]);
test(["foo", "bar"]);
test([3 => "foo", 8 => "bar"]);
test(["foo" => "foo", "bar" => "bar"]);
test(["foo" => "foo", 4 => "bar", 8 => "baz"]);
test(["foo" => "foo", 8 => "baz", 4 => "bar"]);
