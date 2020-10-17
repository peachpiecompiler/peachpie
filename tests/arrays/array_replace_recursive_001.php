<?php
namespace arrays\array_replace_recursive_001;

function test() {
  $a1 = [
    'sub' => [
      'foo' => 'bar'
    ]
  ];

  $a2 = [
    'sub' => [
      'baz' => 'baz'
    ]
  ];

  array_replace_recursive($a1, $a2);

  print_r($a1);
}

test();
