<?php
namespace xml\dom_007;

function test(\DOMNodeList $x) {
  $y =& $x;
  echo $y->length;
}

test(new \DOMNodeList);
