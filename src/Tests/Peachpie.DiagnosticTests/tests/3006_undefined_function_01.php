<?php

function definedFunction($a, $b) {
  return $a + $b;
}

definedFunction(5, 4);

undefinedFunction/*!PHP3006!*/(5, 4);
