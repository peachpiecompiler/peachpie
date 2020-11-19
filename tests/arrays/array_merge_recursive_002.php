<?php
namespace arrays\array_merge_recursive_002;

function test() {
  $a1 = [
    'foo' => 'bar',
    'range' => [
      'px' => [
        'min' => 0,
        'max' => 100,
        'step' => 1
      ]
    ],
    42
  ];

  $a2 = [
    24,
    'range' => [
      'px' => [
        'min' => 1,
        'max' => 200
      ]
    ],
    'baz' => 'baz'
  ];

  $res = array_merge_recursive($a1, $a2);
  $res['range']['px']['step'] = 5;
  $res['other'] = 'other';

  print_r($a1);
  print_r($a2);
  print_r($res);
}

test();
