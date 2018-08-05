<?php

function definedFunction($a, $b) {
  return $a + $b;
}

definedFunction(5, 4);

undefinedFunction/*!PHP5006!*/(5, 4);
