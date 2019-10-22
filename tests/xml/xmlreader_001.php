<?php
namespace xml\xmlreader_001;

function test() {
  $xmlstring = '<?xml version="1.0" encoding="UTF-8"?>
<books></books>';

  $reader = new \XMLReader();
  $reader->XML($xmlstring);

  // Only go through
  while ($reader->read()) {
    echo $reader->name."\n";
  }

  $xmlstring = '';
  $reader = new \XMLReader();
  echo @$reader->XML($xmlstring);
}

test();
