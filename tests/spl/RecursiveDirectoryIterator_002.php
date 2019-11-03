<?php
namespace spl\RecursiveDirectoryIterator_002;

function flags($obj) {
  echo "|";
  if ($obj->isFile()) echo "F";
  if ($obj->isDir()) echo "D";
  if ($obj->isReadable()) echo "R";
  if ($obj->isWritable()) echo "W";
  if ($obj instanceof \DirectoryIterator && $obj->isDot()) echo "O";
  echo "| ";
}

function display($it) {
  $lines = array();
  foreach ($it as $key => $value) {
    ob_start();

    if ($it instanceof \FilesystemIterator) {
      flags($it);
      $path = strtolower($it->getPath());
      echo "[{$it} => {$path}@{$it->getFilename()}]\n";
    }

    flags($value);
    $keyval = is_numeric($key) ? '#' : strtolower($key);  // Order integers are non-deterministic
    $path = strtolower($value->getPath());
    echo "{$keyval} => ". "{$path}@{$value->getFilename()}" ."\n";

    $innerIt = ($it instanceof \RecursiveIteratorIterator) ? $it->getInnerIterator() : $it;
    if ($innerIt instanceof \RecursiveDirectoryIterator) {
      $path = strtolower($innerIt->getSubPath());
      echo "<{$path}@{$innerIt->getSubPathname()}>\n";
    }

    $lines[] = ob_get_clean();
  }
  sort($lines);             // Force displaying files in deterministic order
  echo join("\n", $lines);
  echo "\n\n";
}

function test() {
  $sep = DIRECTORY_SEPARATOR;
  display(new \DirectoryIterator("subdir{$sep}..{$sep}subdir"));
  display(new \FilesystemIterator("subdir{$sep}..{$sep}subdir"));
  display(new \RecursiveDirectoryIterator("subdir{$sep}..{$sep}subdir"));

  display(new \DirectoryIterator(realpath('subdir')));
  display(new \FilesystemIterator(realpath('subdir')));
  display(new \RecursiveDirectoryIterator(realpath('subdir')));

  display(new \DirectoryIterator('subdir'));
  display(new \FilesystemIterator('subdir'));
  display(new \RecursiveDirectoryIterator('subdir'));

  display(new \RecursiveIteratorIterator(new \RecursiveDirectoryIterator('subdir')));

  //display(new GlobIterator(__DIR__ . DIRECTORY_SEPARATOR . 'sampleDir'));
  //display(new GlobIterator(__DIR__ . DIRECTORY_SEPARATOR . 'sampleDir'. DIRECTORY_SEPARATOR .'*'));
  //display(new GlobIterator(__DIR__ . DIRECTORY_SEPARATOR . 'sampleDir'. DIRECTORY_SEPARATOR .'subDir'));
  //display(new GlobIterator(__DIR__ . DIRECTORY_SEPARATOR . 'sampleDir'. DIRECTORY_SEPARATOR .'*'. DIRECTORY_SEPARATOR .'*'));
  //display(new GlobIterator(__DIR__ . DIRECTORY_SEPARATOR . 'sampleDir'. DIRECTORY_SEPARATOR .'file*'));
}

test();
