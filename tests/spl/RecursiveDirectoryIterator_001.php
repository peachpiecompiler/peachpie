<?php

function test() {
  $it = new \RecursiveDirectoryIterator(".", RecursiveDirectoryIterator::CURRENT_AS_SELF);

  $it = new \RecursiveIteratorIterator($it);

  foreach ($it as $file) {
    $name = $file->getFilename();
    if ($name == "." || $name == "..") {
      continue;
    }

    for ($i = 0; $i < $it->getDepth(); $i++) {
      echo "  ";
    }
  	echo $name ."\n";
  }
}

test();
