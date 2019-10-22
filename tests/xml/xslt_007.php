<?php
namespace xml\xslt_007;
// Test 7: Transform To Uri

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

  $doc = $proc->transformToUri($dom, "out.xml");
  print __xml_norm(file_get_contents("out.xml"));
  unlink("out.xml");
}

test();
