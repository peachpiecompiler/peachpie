<?php

function foo() {
  return array(
    0 => 'Bla',
    1 => 'Bal',
    true/*!PHP5022!*/ => 'Alb',
    false/*!PHP5022!*/ => 'Lab',
    42 => 'Foo',
    "42"/*!PHP5022!*/ => 'Bar',
    42.75/*!PHP5022!*/ => 'Baz'
  );
}
