<?php
$xmlstr = <<<XML
<?xml version='1.0' standalone='yes'?>
<numbers>
    <zero>
    nula
    </zero>
    <one value="1">
        <oneAndHalf/>
    </one>
    <two/>
    <three/>
</numbers>
XML;

$sxi = new SimpleXmlIterator($xmlstr);

foreach($sxi as $el)
    echo $el->getName();

echo $sxi;

//print_r($sxi);

?>
