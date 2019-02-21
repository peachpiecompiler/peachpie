<?php

class MyClass {}

class MyIterator implements Iterator {
  function current() { return ""; }
	function key()  { return ""; }
	function next() {}
	function rewind() {}
	function valid() { return false; }
}

class MyArrayIterator extends ArrayIterator {}

function array_or_false($any) {
  return $any ? array('foo' => 'bar') : false;
}

function foo(MyClass $mc, MyIterator $mi, MyArrayIterator $mai, int $i, string $s, float $f, bool $b, array $a, Iterable $it, $any) {
  foreach ($mc as $value) {}
  foreach ($mi as $value) {}
  foreach ($mai as $value) {}
  foreach ($i/*!PHP5024!*/ as $value) {}
  foreach ($s/*!PHP5024!*/ as $value) {}
  foreach ($f/*!PHP5024!*/ as $value) {}
  foreach ($b/*!PHP5024!*/ as $value) {}
  foreach ($a as $value) {}
  foreach ($it as $value) {}
  foreach ($any as $value) {}

  $af = array_or_false($any);
  foreach ($af as $value) {}

  foreach (explode(',', $s) as $value) {}
}
