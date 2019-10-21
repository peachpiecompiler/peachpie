<?php
namespace spl\RecursiveIteratorIterator_001;
$tree = [ [ ["lemon"], "melon" ], [ "orange", "grape" ], "pineapple" ];

$iteriter = new \RecursiveIteratorIterator(new \RecursiveArrayIterator($tree));

foreach ($iteriter as $key => $value) {
  $d = $iteriter->getDepth();
  echo "depth=$d k=$key v=$value\n";
}

echo "Done.";
