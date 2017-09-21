<?php

function test() {
  $dom = new domdocument;
  $dom->load("book.xml");
  $rootNode = $dom->documentElement;
  print "--- Catch exception with try/catch\n";
  try {
    $rootNode->appendChild($rootNode);
  }
  catch (domexception $e) {
    echo $e->getCode();
  }
}

test();
