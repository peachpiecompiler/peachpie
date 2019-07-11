<?php
$dom = new domdocument;
$dom->load("xincludeData/xinclude002a.xml");
$dom->xinclude();
echo $dom -> saveXML();
?>
