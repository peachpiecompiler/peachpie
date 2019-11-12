<?php
namespace classes\interfaces_003;

class A implements \Iterator {
  function rewind() { echo "A"; }
  function next() { }
  function valid() { }
  function key() { }
  function current() { }
}

class B extends A {
  function rewind() { echo "B"; }
}

function test(\Iterator $i) {
  $i->rewind();
}

test(new B);
