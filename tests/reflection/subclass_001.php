<?php

class A {}
class B {}
interface J {}
interface I extends J {}
class C extends B implements I {}

$obj=new ReflectionClass('C');

var_dump($obj->isSubclassOf ('A')); //boolean false
var_dump($obj->isSubclassOf ('B')); //boolean true
var_dump($obj->isSubclassOf ('I')); //boolean true



$i = new ReflectionClass('I');
var_dump($i->isSubclassOf ('J')); //boolean true
