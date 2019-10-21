<?php
namespace variables\valueref_002;

function test() {
  $arr = [1];
  $i = 0;

  $a =& $arr[$i];
  $b = $arr[$i];
  $a = 0;
  
  echo $a, $b, "\n";
  print_r($arr);
}
test();