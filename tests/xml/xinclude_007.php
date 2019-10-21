<?php
namespace xml\xinclude_007;
$dom = new \DOMDocument;
$dom->load("xincludeData/xinclude007a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
