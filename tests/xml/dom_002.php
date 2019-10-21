<?php
namespace xml\dom_002;

function dump(\DOMNodeList $elems) {
	foreach ($elems as $elem) {
    echo "{$elem->nodeType}|{$elem->nodeName} ";

    if ($elem instanceof \DOMCharacterData || $elem instanceof \DOMProcessingInstruction) {
      echo "'". $elem->data ."' ";
    } else if (!is_null($elem->attributes)) {
      foreach ($elem->attributes as $name => $attr) {
        echo "{$name}='{$attr->value}' ";
      }
    }

    if ($elem->hasChildNodes()) {
      dump($elem->childNodes);
    }
	}
}

function test() {
  $xml = <<<HERE
<?xml version="1.0" encoding="ISO-8859-1" ?>
<foo xmlns="http://www.example.com/ns/foo"
     xmlns:fubar="http://www.example.com/ns/fubar">
  <bar fubar:first="lorem" second="ipsum" first="bla">Lorem <u> </u> Ipsum</bar>
  <!-- A comment -->
  <bar> <test2> <![CDATA[Within this Character Data block I can use --, <, &, ', and " as much as I want]]></test2></bar>
  <fubar:bar><test3 /><?xml-stylesheet type="text/xsl" href="styl.xsl"?></fubar:bar>
  <fubar:bar><test4 /></fubar:bar>
</foo>
HERE;

  $dom = new \DOMDocument();
  $dom->loadXML($xml);
  $doc = $dom->documentElement;
  dump($dom->getElementsByTagName('bar'));
  dump($doc->getElementsByTagName('bar'));
  dump($dom->getElementsByTagNameNS('http://www.example.com/ns/fubar', 'bar'));
  dump($doc->getElementsByTagNameNS('http://www.example.com/ns/fubar', 'bar'));
}

test();
