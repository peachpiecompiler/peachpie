<?php
namespace arrays\array_merge_recursive_001;

function test() {
  $a1 = [
    'sub' => [
      'sub' => [
        'foo' => 0
      ]
    ]
  ];

  $a2 = [
    'sub' => [
      'sub' => [
        'foo' => 1
      ]
    ]
  ];

  $res = array_merge_recursive($a1, $a2);

  print_r($a1);
}

test();
