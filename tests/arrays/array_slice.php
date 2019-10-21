<?php
namespace arrays\array_slice;

function test($a, $fn) {
  print_r(array_slice($a, 2, null, true));
  print_r($fn($a, 2, null, true));
}

test(['a' => 'A', 'b' => 'B', 'c' => 'C', 'd' => 'D'], 'array_slice');
