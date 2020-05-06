<?php
namespace strings\is_object_001;

function test() {
  $s = "foo";
  $a =& $s;

  echo (int)\is_object($s);
}

test();
