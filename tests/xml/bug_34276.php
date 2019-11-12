<?php
namespace xml\bug_34276;

function test() {
  $xml = <<<HERE
<?xml version="1.0" encoding="ISO-8859-1" ?>
<foo xmlns="http://www.example.com/ns/foo"
     xmlns:fubar="http://www.example.com/ns/fubar" attra="attra" />
HERE;

  $dom = new \DOMDocument();
  $dom->loadXML($xml);
  $foo = $dom->documentElement;
  echo $foo->hasAttributeNS('http://www.example.com/ns/foo', 'attra');
  echo $foo->getAttributeNS('http://www.example.com/ns/foo', 'attra');

  $foo->setAttributeNS('http://www.example.com/ns/foo', 'attra', 'attranew');
  $foo->setAttributeNS('http://www.example.com/ns/fubar', 'attrb', 'attrbnew');
  $foo->setAttributeNS('http://www.example.com/ns/foo', 'attrc', 'attrc');

  echo $foo->getAttributeNS('http://www.example.com/ns/foo', 'attra');
  echo $foo->getAttributeNS('http://www.example.com/ns/fubar', 'attrb');
  echo $foo->getAttributeNS('http://www.example.com/ns/foo', 'attrc');

  //print $dom->saveXML();
}

test();
