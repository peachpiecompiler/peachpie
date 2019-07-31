<?php
$dom = new domdocument;
$dom->load("xincludeData/xinclude003a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
