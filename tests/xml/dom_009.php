<?php
namespace xml\dom_009;

function test() {
  $doc = new \DOMDocument();
  $doc->loadHTML("hello world"); // no root element
}

test();

echo "Done.";
