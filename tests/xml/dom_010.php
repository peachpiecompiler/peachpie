<?php
namespace xml\dom_010;

function test($xmlStr, $flags = 0) {
  $xml = new \DOMDocument();
  $xml->loadXML($xmlStr);
  $root = $xml->firstChild;
  $valid = $xml->schemaValidate('dom_010.xsd', $flags);
  echo (int)$valid;
  if ($valid) {
    echo ' '. $root->getAttribute('bar') .' '. $root->getAttribute('baz') .' '. $root->getAttribute('bay');
  }
  echo "\n";
}

test('<foo />');
test('<foo />', LIBXML_SCHEMA_CREATE);
test('<fooIncorrect />');
test('<fooIncorrect />', LIBXML_SCHEMA_CREATE);
test('<foo bar="barDefault" />');
test('<foo bar="barDefault" />', LIBXML_SCHEMA_CREATE);
test('<foo bar="barNondefault" />');
test('<foo bar="barNondefault" />', LIBXML_SCHEMA_CREATE);
test('<foo baz="bazFixed" />');
test('<foo baz="bazFixed" />', LIBXML_SCHEMA_CREATE);
test('<foo baz="bazDifferent" />');
test('<foo baz="bazDifferent" />', LIBXML_SCHEMA_CREATE);
test('<foo bay="bayValue" />');
test('<foo bay="bayValue" />', LIBXML_SCHEMA_CREATE);
