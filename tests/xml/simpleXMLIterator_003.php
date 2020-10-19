<?php
// This test was copied from PHP tests. See https://github.com/php/php-src/search?q=SimpleXMLIterator&unscoped_q=SimpleXMLIterator
$xml =<<<EOF
<?xml version='1.0'?>
<!DOCTYPE sxe SYSTEM "notfound.dtd">
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
  <elem111>
   Foo Bar
  </elem111>
 </elem11>
</sxe>
EOF;
$sxe = new SimpleXMLIterator($xml);

for ($sxe->rewind(); $sxe->valid(); $sxe->next()) {
    print_r($sxe->hasChildren());
    print_r(trim($sxe->key()));
    print_r(trim($sxe->current()));
    foreach($sxe->getChildren() as $name => $data) {
        print_r($name);
        print_r(get_class($data));
        print_r(trim($data));
    }
}
?>