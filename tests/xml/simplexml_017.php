<?php
namespace xml\simplexml_017; 

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

  $people = simplexml_load_string($xml);

  foreach($people as $name => $person)
  {
    echo $name ."\n";
    echo (string)$person['name'] ."\n";
    echo count($people) ."\n";
    echo count($person) ."\n";
  }
}

test();
