<?php
namespace xml\xinclude_003;
$dom = new \DOMDocument;
$dom->load("xincludeData/xinclude003a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
