<?php
namespace xml\xinclude_006;
$dom = new \DOMDocument;
$dom->load("xincludeData/xinclude006a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
