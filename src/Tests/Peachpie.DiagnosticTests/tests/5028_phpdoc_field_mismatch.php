<?php

class FieldClass {}

class A {
  var $fAny;

  /**
   * @var FieldClass
   */
  var $fClass;

  /**
   * @var array
   */
  var $fArray;

  /**
   * @var int
   */
  var $fInt;

  /**
   * @var string
   */
  var $fString;

  /**
   * @var resource
   */
  var $fResource;

  /**
   * @var int
   */
  static $fsInt;
}

class B extends A {}

function test(A $a, B $b, FieldClass $fc, string $s, resource $r, $any) {
  $a->fAny = $fc;
  $a->fAny = $any;
  $a->fAny = 42;
  $a->fAny = [ 42 ];

  $a->fClass = $fc;
  $a->fClass = $any;
  $a->fClass = 42/*!PHP5028!*/;
  $a->fClass = [ 42 ]/*!PHP5028!*/;

  $a->fArray = $fc/*!PHP5028!*/;
  $a->fArray = $any;
  $a->fArray = 42/*!PHP5028!*/;
  $a->fArray = [ 42 ];

  $a->fInt = $fc/*!PHP5028!*/;
  $a->fInt = $any;
  $a->fInt = 42;
  $a->fInt = [ 42 ]/*!PHP5028!*/;

  $a->fString = $s;
  $a->fString = $s . " foo";

  $a->fResource = $r;
  $a->fResource = $fc;
  $a->fResource = 42/*!PHP5028!*/;
  $a->fResource = [ $r ]/*!PHP5028!*/;
  $a->fResource = fopen("somefile", "r");

  $b->fInt = $fc/*!PHP5028!*/;
  A::$fsInt = 42;
}
