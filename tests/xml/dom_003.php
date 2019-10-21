<?php
namespace xml\dom_003;

function test() {
  $dom = new \DOMDocument;
  $dom->load("book.xml");
  $rootNode = $dom->documentElement;
  print "--- Catch exception with try/catch\n";
  try {
    $rootNode->appendChild($rootNode);
  }
  catch (\DOMException $e) {
    echo $e->getCode();
  }
}

test();
