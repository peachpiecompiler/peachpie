<?php
namespace xml\simplexml_019; 

function test() {
  $xml =<<<EOF
<people>
  <person name="Joe"/>
  <person name="John">
    <children>
      <person name="Joe"/>
    </children>
  </person>
  <person name="Jane"/>
</people>
EOF;

  $xml1 =<<<EOF
<people>
  <person name="John">
    <children>
      <person name="Joe"/>
    </children>
  </person>
  <person name="Jane"/>
</people>
EOF;

  $people = simplexml_load_string($xml);
  $people1 = simplexml_load_string($xml);
  $people2 = simplexml_load_string($xml1);

  echo ($people1 == $people) ? "TRUE\n" : "FALSE\n";
  echo ($people2 == $people) ? "TRUE\n" : "FALSE\n";
  echo ($people2 == $people1) ? "TRUE\n" : "FALSE\n";
}

test();
