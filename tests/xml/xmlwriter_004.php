<?php
namespace xml\xmlwriter_004;

function normalize($xml) {
  return trim(str_replace(array("UTF-8", " />"), array("utf-8", "/>"), $xml));
}

function test() {
  $xw = new \XMLWriter();
  $xw->openMemory();
  $xw->setIndent(false);
  $xw->startDocument('1.0', 'utf-8', 'no');
  $xw->writeDtd("DTD<");
  $xw->writeDtd("DTD","public1","<system");
  $xw->writeDtd("DTD","public","system","<subset");
  $xw->startDtd('foo', NULL, 'urn:bar');
  $xw->startDtdElement("el1");
  $xw->text('<">"');
  $xw->startDtdElement("el2");
  $xw->endDtd();
  $xw->startElement("tag");
  $xw->endElement();
  $xw->endDocument();

  // Force to write and empty the buffer
  echo normalize($xw->flush(true));
}

test();
