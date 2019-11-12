<?php
namespace xml\dom_001;

$xml = <<<HERE
<?xml version="1.0" ?>
<root a="b" />
HERE;

$xml2 = <<<HERE
<?xml version="1.0" ?>
<doc2 />
HERE;

$dom = new \DOMDocument();
$dom->loadXML($xml);
$root = $dom->documentElement;
$attr = $root->getAttributeNode('a');

$dom2 = new \DOMDocument();
$dom2->loadXML($xml2);
$root2 = $dom2->documentElement;
try {
  $root2->setAttributeNode($attr);
}
catch (\DOMException $e) {
  echo $e->getCode();
}
