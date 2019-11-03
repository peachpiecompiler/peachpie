<?php
namespace arrays\alias_002;

function set(&$alias, $value) {
  $alias = $value;
}

function test() {
  $arr = ['foo' => null];
  set($arr['foo'], ['bar']);
  
  print_r(str_replace('a', 'o', $arr['foo']));
}

test();
