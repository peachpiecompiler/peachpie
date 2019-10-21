<?php
namespace arrays\alias_001;

function test() {
  $arr = [ true ];
  $i = 0;

  $first_ref =& $arr[$i];
  $first = $arr[$i];
  $arr[$i] = false;
  
  echo (int)$first_ref ."\n";
  echo (int)$first ."\n";
  print_r($arr);
}

test();
