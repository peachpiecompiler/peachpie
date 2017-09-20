<?php
// Test 5: Checking Indent

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

  $xp = new domxpath($xsl);
  $res = $xp->query("/xsl:stylesheet/xsl:output/@indent");
  if ($res->length != 1) {
    print "No or more than one xsl:output/@indent found";
    exit;
  }
  $res->item(0)->value = "yes";

  $proc = new xsltprocessor;
  $proc->importStylesheet($xsl);

  print __xml_norm($proc->transformToXml($dom));
}

test();
