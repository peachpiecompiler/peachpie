<?php
namespace arrays\foreach_003;

//
// Tests whether PhpArray's foreach enumerator correctly 
// dereferences and deeply copies values.
//

$x = array(1,2,3);
$a = array("x" => &$x);
foreach ($a as $k => $v)
{
  $x[1] = 10;
  echo "$k => ";
  print_r($v);
}
