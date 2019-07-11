<?php
$dom = new domdocument;
$dom->load("xincludeData/xinclude004a.xml");
$dom->xinclude();
echo $dom -> saveXML();
?>
