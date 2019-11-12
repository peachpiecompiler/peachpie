<?php
namespace xml\xpath_001;

$xml = <<<HERE
<?xml version="1.0" encoding="ISO-8859-1" ?>
<foo>
  <bar second="ipsum" first="bla">Lorem <u>Ipsum</u></bar>
  <!-- A comment -->
  <bar><test2></test2></bar>
  <bar><test3 /></bar>
</foo>
HERE;

function dump($elems) {
  foreach ($elems as $elem) {
    echo "{$elem->nodeName}: {$elem->nodeValue}, ";
  }
}

$dom = new \DOMDocument();
$dom->loadXML($xml);
$xpath = new \DOMXPath($dom);

$bars = $xpath->query('//bar');
dump($bars);

$bar1 = $bars->item(0);
dump($xpath->query('u'));
dump($xpath->query('u', $bar1));
