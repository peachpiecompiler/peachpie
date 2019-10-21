<?php
namespace xml\xinclude_001;
$dom = new \DOMDocument;
$dom->load("xincludeData/xinclude001a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
