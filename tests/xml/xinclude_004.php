<?php
namespace xml\xinclude_004;
$dom = new \DOMDocument;
$dom->load("xincludeData/xinclude004a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
