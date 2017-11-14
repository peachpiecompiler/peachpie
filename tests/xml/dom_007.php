<?php

function test(DOMNodeList $x) {
  $y =& $x;
  echo $y->length;
}

test(new DOMNodeList);
