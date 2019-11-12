<?php
namespace xml\simplexml_005; 

function test() {
  $sxe = simplexml_load_string(<<<EOF
<?xml version='1.0'?>
<sxe id="elem1">
 Plain text.
 <elem1 attr1='first'>
  Bla bla 1.
  <!-- comment -->
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
 </elem11>
</sxe>
EOF
  );

  foreach($sxe->children() as $name=>$val) {
    echo $name ."\n";
    echo get_class($val) ."\n";
    echo trim($val) ."\n";
  }
}

test();
