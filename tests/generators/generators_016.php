<?php
namespace generators\generators_016;

function test() {
  yield 1;

  if (false) {
    yield 24;
  } else {
    yield 42;
  }
}

foreach (test() as $val) {
  echo $val;
}
