<?php
namespace xml\dom_006;

function test() {
  $html = "<div>data</div>";
  $doc = new \DOMDocument;
  $doc->loadHTML($html);
  
  $list = $doc->getElementsByTagName('div');
  
  echo isset($list[0]) ? "TRUE" : "FALSE";
  echo $list[0]->textContent;
  
  echo isset($list["0"]) ? "TRUE" : "FALSE";
  echo $list["0"]->textContent;
  
  echo isset($list["0.1234"]) ? "TRUE" : "FALSE";
  echo $list["0.1234"]->textContent;
  
  echo isset($list["some string"]) ? "TRUE" : "FALSE";
  echo $list["some string"]->textContent;
  
  echo isset($list[1]) ? "TRUE" : "FALSE";
  
  echo isset($list["1.5"]) ? "TRUE" : "FALSE";
}

test();
