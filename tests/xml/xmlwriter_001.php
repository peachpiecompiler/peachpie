<?php
namespace xml\xmlwriter_001;

function normalize($xml) {
  return trim(str_replace(array("UTF-8", " />"), array("utf-8", "/>"), $xml));
}

function test() {
  $xw = new \XMLWriter();
  $xw->openMemory();
  $xw->startDocument('1.0', 'utf-8', 'no');
  $xw->startElement("tag1");
  $xw->endDocument();

  // Force to write and empty the buffer
  echo normalize($xw->flush(true));
}

test();
