<?php

function test() {
  $it = new \RecursiveDirectoryIterator(".", RecursiveDirectoryIterator::CURRENT_AS_SELF);

  $it = new \RecursiveIteratorIterator($it);

  foreach ($it as $file) {
    for ($i = 0; $i < $it->getDepth(); $i++) {
      echo "  ";
    }
  	echo $file->getFilename() ."\n";
  }
}

test();
