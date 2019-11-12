<?php
namespace xml\xslt_006;
// Test 6: Transform To Doc

function __xml_norm($str)
{
  $str = str_replace(array(" /", "?><", "\r\n"), array("/", "?>\n<", "\n"), $str);

  if ($str[strlen($str) - 1] != "\n") $str = $str . "\n";

  return $str;
}

function test() {
  $dom = new \DOMDocument;
  $dom->load("xslt.xml");

  $xsl = new \DOMDocument;
  $xsl->load("xslt.xsl");

  $proc = new \XSLTProcessor;
  $proc->importStylesheet($xsl);

  $doc = $proc->transformToDoc($dom);
  print __xml_norm($doc->saveXML());
}

test();
