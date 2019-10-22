<?php
namespace xml\xpath_002;

$xml = <<<HERE
<?xml version="1.0" encoding="ISO-8859-1" ?>
<foo xmlns="http://www.example.com/ns/foo"
     xmlns:fubar="http://www.example.com/ns/fubar">
  <fubar:bar fubar:first="lorem" second="ipsum" first="bla">Lorem <u>Ipsum</u> <fubar:u>Dolor</fubar:u></fubar:bar>
  <!-- A comment -->
  <bar>test2</bar>
  <bar>test3</bar>
</foo>
HERE;

function dump($elems) {
  if ($elems) {
    foreach ($elems as $elem) {
      echo "{$elem->nodeName}: {$elem->nodeValue}, ";
    }
  }

  echo "--- ";
}

$dom = new \DOMDocument();
$dom->loadXML($xml);
$xpath = new \DOMXPath($dom);

$bars = $xpath->query('//bar');
dump($bars);

$bars = $xpath->query('//fubar:bar');
dump($bars);

$bar1 = $bars->item(0);
dump($xpath->query('u', $bar1));
dump($xpath->query('fubar:u', $bar1));
dump($xpath->query('u', $bar1, false));
dump(@$xpath->query('fubar:u', $bar1, false));

$xpath->registerNamespace('fubar', 'http://www.example.com/ns/fubar');
dump($xpath->query('fubar:u', $bar1, false));