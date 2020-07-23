<?php
namespace xml\xmlwriter_006;

function normalize($xml) {
  return trim(str_replace(array("UTF-8", " />"), array("utf-8", "/>"), $xml));
}

function test() {
  $xw = new \XMLWriter();
  $xw->openMemory(); 
  
  $xw->setIndent(true);
  $xw->setIndentString("");
  $xw->startDocument(NULL, "UTF-8");
  $xw->writeDtdEntity("name","a");
  $xw->startDtdEntity("name",true);
  $xw->text('elem2*');
  $xw->endDtdEntity();
  $xw->writeDtdEntity("name","<a", true);
  $xw->writeDtdEntity("name","<a", false, "b", "c");
  $xw->writeDtdEntity("name","a", false, "b", "c", "d");
  $xw->writeDtdElement('sxe', '(elem1+, elem11, elem22*)');
  $xw->writeDtdAttlist('sxe', 'id     CDATA  #implied');
  $xw->startDtdElement('elem1');
  $xw->text('elem2*');
  $xw->text('elem2+');
  $xw->endDtdElement();
  $xw->startDtdAttlist('elem1');
  $xw->writeDtd("DTD<");
  $xw->text("attr1  CDATA  #required\n");
  $xw->text('attr2  CDATA  #implied');
  $xw->endDtdAttlist();
  $xw->writeDtdElement('sxe', '(elem1+, elem11, elem22*)');
  $xw->endDocument();

  // Force to write and empty the buffer
  echo normalize($xw->flush(true));
}

test();

