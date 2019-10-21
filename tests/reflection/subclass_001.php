<?php
namespace reflection\subclass_001;

class A {}
class B {}
interface J {}
interface I extends J {}
class C extends B implements I {}

function test() {
    $obj=new \ReflectionClass('reflection\\subclass_001\\C');

  print_r($obj->isSubclassOf ('reflection\\subclass_001\\A')); //boolean false
  print_r($obj->isSubclassOf ('reflection\\subclass_001\\B')); //boolean true
  print_r($obj->isSubclassOf ('reflection\\subclass_001\\I')); //boolean true



  $i = new \ReflectionClass('reflection\\subclass_001\\I');
  print_r($i->isSubclassOf ('reflection\\subclass_001\\J')); //boolean true
}

test();
