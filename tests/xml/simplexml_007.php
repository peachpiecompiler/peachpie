<?php
namespace xml\simplexml_007; 

function test() {
  $xml =<<<EOF
<?xml version="1.0" encoding="ISO-8859-1" ?>
<foo>bar<baz/>bar</foo>
EOF;

  $sxe = simplexml_load_string($xml);

  echo (string)$sxe;
}

test();
