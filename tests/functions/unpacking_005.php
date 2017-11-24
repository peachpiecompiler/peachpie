<?php

function test(...$args) {
  $args = $args[0];
  print_r($args);
}

test(666);
