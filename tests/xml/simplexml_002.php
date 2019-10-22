<?php
namespace xml\simplexml_002;

function __norm($text) {
  return str_replace("\r\n", "\n", $text);
}

function test() {
  $sxe = simplexml_load_string(<<<EOF
<?xml version='1.0'?>
<sxe id="elem1">
 Plain text.
 <elem1 attr1='first'>
  Bla bla 1.
  <elem2>
   Here we have some text data.
   <elem3>
    And here some more.
    <elem4>
     Wow once again.
    </elem4>
   </elem3>
  </elem2>
 </elem1>
 <elem11 attr2='second'>
  Bla bla 2.
  <elem111>
   Foo Bar
  </elem111>
 </elem11>
</sxe>
EOF
  );

  foreach($sxe as $name => $data) {
    echo $name;
    echo __norm($data);
  }

  echo "===CLONE===\n";

  foreach(clone $sxe as $name => $data) {
    echo $name;
    echo trim(__norm($data));
  }

  echo "===ELEMENT===\n";

  foreach($sxe->elem11 as $name => $data) {
    echo $name;
    echo trim(__norm($data));
  }
}

test();
