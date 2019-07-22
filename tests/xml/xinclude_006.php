<?php
$dom = new domdocument;
$dom->load("xincludeData/xinclude006a.xml");
@$dom->xinclude();
echo $dom -> saveXML();
?>
