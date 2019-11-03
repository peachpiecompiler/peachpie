<?php
namespace functions\unpacking_005;

function test(...$args) {
  $args = $args[0];
  print_r($args);
}

test(666);
