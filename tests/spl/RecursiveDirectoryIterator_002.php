<?php

function flags($obj) {
  echo "|";
  if ($obj->isFile()) echo "F";
  if ($obj->isDir()) echo "D";
  if ($obj instanceof DirectoryIterator && $obj->isDot()) echo "O";
  echo "| ";
}

function display($it) {
  $lines = array();
  foreach ($it as $key => $value) {
    ob_start();

    if ($it instanceof FilesystemIterator) {
      flags($it);
      echo "[{$it} => {$it->getPath()}@{$it->getFilename()}]\n";
    }

    flags($value);
    echo "{$key} => ". "{$value->getPath()}@{$value->getFilename()}" ."\n";

    if ($it instanceof RecursiveIteratorIterator) {
      $it = $it->getInnerIterator();
    }

    if ($it instanceof RecursiveDirectoryIterator) {
      echo "<{$it->getSubPath()}@{$it->getSubPathname()}>\n";
    }

    $lines[] = ob_get_clean();
  }
  sort($lines);             // Force displaying files in deterministic order
  echo join("\n", $lines);
  echo "\n\n";
}

function test() {
  $sep = DIRECTORY_SEPARATOR;
  display(new DirectoryIterator("subdir{$sep}..{$sep}subdir"));
  display(new FilesystemIterator("subdir{$sep}..{$sep}subdir"));
  display(new RecursiveDirectoryIterator("subdir{$sep}..{$sep}subdir"));

  display(new DirectoryIterator(realpath('subdir')));
  display(new FilesystemIterator(realpath('subdir')));
  display(new RecursiveDirectoryIterator(realpath('subdir')));

  display(new DirectoryIterator('subdir'));
  display(new FilesystemIterator('subdir'));
  display(new RecursiveDirectoryIterator('subdir'));

  display(new RecursiveIteratorIterator(new RecursiveDirectoryIterator('subdir')));
}

test();
