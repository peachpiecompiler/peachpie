<?php
namespace xml\xinclude_002;
$dom = new \DOMDocument;
$dom->load("xincludeData/xinclude002a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
