<?php
namespace variables\isset_004;

function test(array $a, $s) {
  echo isset($a['foo' . $s]);
}

test(['foobar' => 42], "bar");

echo "Done.";
