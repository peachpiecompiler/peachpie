<?php

function test() {
  $dom = new domDocument;
  $dom->load("area_name.xml");
  if(!$dom) {
    echo "Error while parsing the document\n";
    exit;
  }
  $xsl = new domDocument;
  $xsl->load("area_list.xsl");
  if(!$xsl) {
    echo "Error while parsing the document\n";
    exit;
  }
  $proc = new xsltprocessor;

  if($proc === false) {
    echo "Error while making xsltprocessor object\n";
    exit;
  }

  $proc->importStylesheet($xsl);
  print str_replace("\r\n", "\n", $proc->transformToXml($dom));

  //this segfaulted before
  // WTF? there's no sibling...
  //print $dom->documentElement->firstChild->nextSibling->nodeName;
}

test();
