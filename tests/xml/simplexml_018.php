<?php
namespace xml\simplexml_018;

function test() {
  $xml =<<<EOF
<root s:att1="b" att1="a" 
      xmlns:s="urn::test" xmlns:t="urn::test-t">
   <child1>test</child1>
   <child1>test 2</child1>
   <s:child3 />
</root>
EOF;

  $sxe = simplexml_load_string($xml);

  echo $sxe->child1[0]."\n";
  echo $sxe->child1[1]."\n\n";

  echo isset($sxe->child1[1]) ? "TRUE\n" : "FALSE\n";
  unset($sxe->child1[1]);
  echo isset($sxe->child1[1]) ? "TRUE\n" : "FALSE\n";
  echo "\n";

  $atts = $sxe->attributes("urn::test");
  echo isset($atts[0]) ? "TRUE\n" : "FALSE\n";
  unset($atts[0]);
  echo isset($atts[0]) ? "TRUE\n" : "FALSE\n";
  echo isset($atts[TRUE]) ? "TRUE\n" : "FALSE\n";
}

test();
