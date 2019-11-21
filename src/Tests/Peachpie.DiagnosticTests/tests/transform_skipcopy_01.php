<?php

function test1($x, $y) {
  $x1 = $x/*{skipCopy:1}*/;
  $y1 = $y/*{skipCopy:0}*/;

  $y1[0] = 'c';

  $x1 = $y1/*{skipCopy:0}*/;
  $y1[1] = 'd';
}

function test2($x, $y) {
  $x1 = $x/*{skipCopy:0}*/;
  $y1 = $y/*{skipCopy:1}*/;

  for ($i = 0; $i < 10; ++$i) {
    if ($i % 2 == 0) {
      $x1[0] = 1;
    } else {
      $x1 = $y/*{skipCopy:0}*/;
    }

    echo $y;
  }
}

function test3($x) {
  $a = $b = $x/*{skipCopy:1}*/;
  echo $x;
}
