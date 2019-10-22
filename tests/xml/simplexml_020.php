<?php
namespace xml\simplexml_020;

function test() {
  $xml =<<<EOF
<people>
test
  <person name="Joe"/>
  <person name="John">
    <children>
      <person name="Joe"/>
    </children>
  </person>
  <person name="Jane"/>
</people>
EOF;

  $foo = simplexml_load_string( "<foo />" );
  $people = simplexml_load_string($xml);

  echo (bool)$foo ? "TRUE\n" : "FALSE\n";
  echo (bool)$people ? "TRUE\n" : "FALSE\n";
  echo (int)$foo ."\n";
  echo (int)$people ."\n";
  echo (double)$foo ."\n";
  echo (double)$people ."\n";
  echo trim((string)$foo) ."\n";
  echo trim((string)$people) ."\n";
  //print_r((array)$foo);
  //print_r((array)$people);
  //print_r((object)$foo);
  //print_r((object)$people);
}

test();
