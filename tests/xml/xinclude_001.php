<?php
$dom = new domdocument;
$dom->load("xincludeData/xinclude001a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
