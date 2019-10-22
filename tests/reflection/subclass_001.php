<?php
namespace reflection\subclass_001;

class A {}
class B {}
interface J {}
interface I extends J {}
class C extends B implements I {}

function test() {
    $obj=new \ReflectionClass(__NAMESPACE__ . "\\C");

  print_r($obj->isSubclassOf (__NAMESPACE__ . "\\A")); //boolean false
  print_r($obj->isSubclassOf (__NAMESPACE__ . "\\B")); //boolean true
  print_r($obj->isSubclassOf (__NAMESPACE__ . "\\I")); //boolean true



  $i = new \ReflectionClass(__NAMESPACE__ . "\\I");
  print_r($i->isSubclassOf (__NAMESPACE__ . "\\J")); //boolean true
}

test();
