<?php
namespace xml\xslt_008;
// Test 8: php:function Support

function __xml_norm($str)
{
  $str = str_replace(array(" /", "?><", "\r\n"), array("/", "?>\n<", "\n"), $str);

  if ($str[strlen($str) - 1] != "\n") $str = $str . "\n";

  return $str;
}

Class foo {
    function __construct() {}
    function __toString() { return "not a DomNode object";}
}

function foobar($id, $secondArg = "" ) {
  if (is_array($id)) {
      return $id[0]->value . " - " . $secondArg;
  } else {
      return $id . " - " . $secondArg;
  }
}

function nodeSet($id = null) {
  if ($id and is_array($id)) {
      return $id[0];
  } else {
      $dom = new \DOMDocument;
      $dom->loadXML("<root>this is from an external \DOMDocument</root>");
      return $dom->documentElement;
  }
}

function nonDomNode() {
  return new foo();
}

class aClass {
  static function aStaticFunction($id) {
      return $id;
  }
}

function test() {
  $dom = new \DOMDocument();
  $dom->load("xslt_008.xsl");
  $proc = new \XSLTProcessor;
  $xsl = $proc->importStylesheet($dom);

  $xml = new \DOMDocument();
  $xml->load("xslt_008.xml");
  $proc->registerPHPFunctions();
  print __xml_norm($proc->transformToXml($xml));
}

test();
