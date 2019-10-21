<?php
namespace xml\bug_32615;

function __xml_norm($str) {
  $str = str_replace(array(" /", "?><", "\r\n"), array("/", "?>\n<", "\n"), $str);

  if ($str[strlen($str) - 1] != "\n") $str = $str . "\n";

  return $str;
}

function test() {
  $dom = new \DOMDocument;
  $dom->formatOutput = true;

  $frag = $dom->createDocumentFragment();
  $frag->appendChild(new \DOMElement('root'));
  $dom->appendChild($frag);
  $root = $dom->documentElement;

  $frag->appendChild(new \DOMElement('first'));
  $root->appendChild($frag);

  $frag->appendChild(new \DOMElement('second'));
  $root->appendChild($frag);

  $node = $dom->createElement('newfirst');
  $frag->appendChild($node);
  $root->replaceChild($frag, $root->firstChild);

  unset($frag);
  $frag = $dom->createDocumentFragment();

  $frag->appendChild(new \DOMElement('newsecond'));
  $root->replaceChild($frag, $root->lastChild);

  $node = $frag->appendChild(new \DOMElement('fourth'));
  $root->insertBefore($frag, NULL);

  $frag->appendChild(new \DOMElement('third'));
  $node = $root->insertBefore($frag, $node);

  $frag->appendChild(new \DOMElement('start'));
  $root->insertBefore($frag, $root->firstChild);

  $frag->appendChild(new \DOMElement('newthird'));
  $root->replaceChild($frag, $node);

  $frag->appendChild(new \DOMElement('newfourth'));
  $root->replaceChild($frag, $root->lastChild);

  $frag->appendChild(new \DOMElement('first'));
  $root->replaceChild($frag, $root->firstChild->nextSibling);

  $root->removeChild($root->firstChild);

  echo __xml_norm($dom->saveXML());

  while ($root->hasChildNodes()) {
    $root->removeChild($root->firstChild);
  }

  $frag->appendChild(new \DOMElement('first'));
  $root->insertBefore($frag, $root->firstChild);

  $node = $frag->appendChild(new \DOMElement('fourth'));
  $root->appendChild($frag);

  $frag->appendChild(new \DOMElement('second'));
  $frag->appendChild(new \DOMElement('third'));
  $root->insertBefore($frag, $node);

  echo __xml_norm($dom->saveXML());

  $frag = $dom->createDocumentFragment();
  $root = $dom->documentElement;
  $root->replaceChild($frag, $root->firstChild);

  echo __xml_norm($dom->saveXML());
}

test();
