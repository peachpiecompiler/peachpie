<?php

class A {}
class B extends A {}

/**
 * @param int $i Some parameter.
 * @param int $j Some parameter.
 * @param array $k Some parameter.
 * @param int[] $l Some parameter.
 * @param int[]|A[] $m Some parameter.
 * @param int|A $n Some parameter.
 * @param int|A $o Some parameter.
 * @param A $p Some parameter.
 * @param A $q Some parameter.
 * @param B $r Some parameter.
 * @param B $s Some parameter.
 * @param A $t Some parameter.
 */
function foo(
  int $i,
  float $j/*!PHP5027!*/,
  array $k,
  array $l,
  array $m,
  $n,
  A $o/*!PHP5027!*/,
  A $p,
  B $q/*!PHP5027!*/,
  A $r,
  B $s,
  A &$t,
  array $u)
{}

class Bar extends B {
  /**
   * @param A $a Some parameter.
   * @param self $b Some parameter.
   */
  public function boo(self $a/*!PHP5027!*/, A $b) {}
}
