<?php
// Test 6: Transform To Doc

function __xml_norm($str)
{
  $str = str_replace(array(" /", "?><", "\r\n"), array("/", "?>\n<", "\n"), $str);

  if ($str[strlen($str) - 1] != "\n") $str = $str . "\n";

  return $str;
}

function test() {
  $dom = new domDocument;
  $dom->load("xslt.xml");

  $xsl = new domDocument;
  $xsl->load("xslt.xsl");

  $proc = new xsltprocessor;
  $proc->importStylesheet($xsl);

  $doc = $proc->transformToDoc($dom);
  print __xml_norm($doc->saveXML());
}

test();
