<?php
namespace operators\comparison_002;

function test(string $s = null) {
  echo (int)($s === null);
  echo (int)($s !== null);
}

test(null);
