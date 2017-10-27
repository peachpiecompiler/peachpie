<?php

class A {}
class B {}
interface J {}
interface I extends J {}
class C extends B implements I {}

function test() {
  $obj=new ReflectionClass('C');

  print_r($obj->isSubclassOf ('A')); //boolean false
  print_r($obj->isSubclassOf ('B')); //boolean true
  print_r($obj->isSubclassOf ('I')); //boolean true



  $i = new ReflectionClass('I');
  print_r($i->isSubclassOf ('J')); //boolean true
}

test();
