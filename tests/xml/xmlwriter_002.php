<?php
namespace xml\xmlwriter_002;

function normalize($xml) {
  return trim(str_replace(array("UTF-8", " />"), array("utf-8", "/>"), $xml));
}

function test() {
  $xw = new \XMLWriter();
  $xw->openMemory();
  $xw->startDocument('1.0', 'utf-8');
  $xw->startElement("tag1");

  $res = $xw->startAttribute('attr1');
  $xw->text("attr1_value");
  $xw->endAttribute();

  $res = $xw->startAttribute('attr2');
  $xw->text("attr2_value");
  $xw->endAttribute();

  $xw->text("Test text for tag1");
  $res = $xw->startElement('tag2');
  if ($res < 1) {
    echo "StartElement context validation failed\n";
    exit();
  }
  $xw->endDocument();

  // Force to write and empty the buffer
  echo normalize($xw->flush(true));
}

test();
