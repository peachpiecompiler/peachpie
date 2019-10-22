<?php
namespace spl\RecursiveDirectoryIterator_001;

function test() {
  $it = new \RecursiveDirectoryIterator(".", \RecursiveDirectoryIterator::CURRENT_AS_SELF);

  $it = new \RecursiveIteratorIterator($it);

  foreach ($it as $file) {
    if ($file->isDot()) {
      continue;
    }

    for ($i = 0; $i < $it->getDepth(); $i++) {
      echo "  ";
    }
  	echo $file->getFilename() ."\n";
  }
}

test();
