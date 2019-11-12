<?php
namespace xml\dom_004;

class books extends \DOMDocument {
  function addBook($title, $author) {
    $titleElement = $this->createElement("title");
    $titleElement->appendChild($this->createTextNode($title));
    $authorElement = $this->createElement("author");
    $authorElement->appendChild($this->createTextNode($author));

    $bookElement = $this->createElement("book");

    $bookElement->appendChild($titleElement);
    $bookElement->appendChild($authorElement);
    $this->documentElement->appendChild($bookElement);
  }
}

function test() {
  $dom = new books;
  $dom->formatOutput = true;

  $dom->load("book.xml");
  $dom->addBook("PHP de Luxe", "Richard Samar, Christian Stocker");
  echo str_replace("\r\n", "\n", $dom->saveXML());
}

test();
