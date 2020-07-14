<?php
namespace xml\xmlwriter_005;

function normalize($xml) {
  return trim(str_replace(array("UTF-8", " />"), array("utf-8", "/>"), $xml));
}

function test() {
  $xw = new \XMLWriter();
  $xw->openMemory();
  $xw->writeDtd("foo",NULL,NULL,"<!ELEMENT el>");
  
  $xw->startDtd('foo', NULL, 'urn:bar');
  $xw->endDtd();
  $xw->startElement('foo');
  $xw->writeElementNS('foo', 'bar', 'urn:foo', 'dummy content');
  $xw->endElement();

  // Force to write and empty the buffer
  echo normalize($xw->flush(true));
}

test();
