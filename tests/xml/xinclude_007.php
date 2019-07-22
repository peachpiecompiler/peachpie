<?php
$dom = new domdocument;
$dom->load("xincludeData/xinclude007a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
