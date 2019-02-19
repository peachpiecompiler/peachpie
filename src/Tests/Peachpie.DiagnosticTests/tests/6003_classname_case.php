<?php

class MyClass {}

function foo() {
  $ai = new ArrayIterator();
  $ai = new arrayiterator/*!PHP6003!*/();
  $ai = new arrayIterator/*!PHP6003!*/();
  $ai = new ARRAYITERATOR/*!PHP6003!*/();

  $mc = new MyClass();
  $mc = new MYCLASS/*!PHP6003!*/();
}
