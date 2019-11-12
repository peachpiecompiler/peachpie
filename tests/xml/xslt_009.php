<?php
namespace xml\xslt_009;
// Test 9: Using Associative Array of Parameters

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
  $xsl->load("xslt_009.xsl");

  $proc = new \XSLTProcessor;
  $proc->importStylesheet($xsl);

  $parameters = Array(
    'foo' => 'barbar',
    'foo1' => 'test',
  );

  $proc->setParameter("", $parameters);

  print __xml_norm($proc->transformToXml($dom));
}

test();
