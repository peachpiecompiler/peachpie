<?php
namespace arrays\array_map;

  function f($x,$y,&$z)
  {
    $result = "$x $y $z";
    $z = 'x';
    return $result;
  }

  function g($x)
  {
    return $x+1;
  }
  
  $a = array(1,2,3);
  $b = array('A','B','C');
  $c = array(0,0);
  $d = array('A' => 1,10 => 2);
  
  print_r(array_map(null,$a,$b,$c));
  print_r(array_map(__NAMESPACE__ . "\\f",$a,$b,$c));
  print_r(array_map(__NAMESPACE__ . "\\g",$d));

  print_r($a);
  print_r($b);
  print_r($c);
  print_r($d);
