<?php
namespace spl\OuterIterator_001;

class NoDotFilterIterator extends \FilterIterator
{
  public function accept() {
    // isDot is called on the underlying \DirectoryIterator
    return !$this->isDot();
  }
}

function test() {
  $dirIt = new \DirectoryIterator("subdir");
  $recDirIt = new \RecursiveDirectoryIterator("subdir");

  $filtIt = new NoDotFilterIterator($dirIt);
  foreach ($filtIt as $file) {
    echo $file ."\n";
  }
  echo "-----\n";

  foreach ($dirIt as $file) {
    if (!$dirIt->isDot()) {
      echo $file ."\n";
    }
  }
  echo "-----\n";

  $recDirItIt = new \RecursiveIteratorIterator($recDirIt);
  foreach ($recDirItIt as $file) {
    if (!$recDirItIt->isDot()) {
      echo $recDirItIt->getFilename() ."\n";
    }
  }
}

test();
