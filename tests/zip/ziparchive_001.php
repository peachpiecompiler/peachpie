<?php
namespace zip\ziparchive_001;

function test($filename) {
  // Delete the archive file possibly hanging there from previous test
  if (file_exists($filename)) {
    unlink($filename);
  }

  // Create the archive
  $zip = new \ZipArchive();
  if ($zip->open($filename, \ZipArchive::CREATE)!==TRUE) {
    exit("cannot open <$filename>\n");
  }

  // Currently, only the version of PHP running on Travis (>= 7.4.4) works with $start and $length
  // in ZipArchive::addFile correctly.
  // TODO: Remove this when Azure DevOps support it as well.
  if (getenv("TRAVIS") == "true") {
    $ipsumStart = 2;
    $ipsumLength = 5;
  } else {
    $ipsumStart = $ipsumLength = 0;
  }

  // Fill the archive
  $zip->addFromString("testfilephp.txt", "#1 This is a test string added as testfilephp.txt.\n");
  $zip->addFromString("testfilephp2.txt", "#2 This is a test string added as testfilephp2.txt.\n");
  $zip->addFile("ziparchive_001.txt", "ipsum.txt", $ipsumStart, $ipsumLength); // $start and $length must be ignored
  echo "filename: ". strtolower($zip->filename) ."\n";
  echo "numfiles: " . $zip->numFiles . "\n";
  //echo "status:" . $zip->status . "\n";
  $zip->close();

  // Open the archive again and list its contents
  list_entries($filename);

  // Clean up the created archive
  unlink($filename);
}

function list_entries($filename) {
  $zip = new \ZipArchive();
  if ($zip->open($filename)!==TRUE) {
    exit("cannot open <$filename>\n");
  }

  for ($i = 0; $i < $zip->numFiles; $i++) {
    $stats = $zip->statIndex($i);
    print_r(clear_stats($stats));

    $name = $zip->getNameIndex($i);
    $index = $zip->locateName($name);
    $stats_eq = ($stats === $zip->statName($name));
    echo $i ."|". $name ."|". $index ."|". $stats_eq ."\n";

    echo $zip->getFromIndex($i) ."\n";
    echo $zip->getFromName($name) ."\n";

    $stream = $zip->getStream($name);
    echo stream_get_contents($stream) ."\n\n";
  }

  $zip->close();
}

function clear_stats($stats) {
  // Currently not supported
  unset($stats['crc']);
  unset($stats['encryption_method']);

  // .NET \ZipArchive always uses deflating, even if it's inefficient and no compression would be better
  // (if not specified manually, but it is hard to guess beforehand)
  if ($stats['comp_size'] >= $stats['size']) {
    unset($stats['comp_size']);
    unset($stats['comp_method']);
  }

  // Entry creation time is non-deterministic (PHP and .NET runs of the test can happen on the edge of a second),
  // the only exception is the file inserted from the filesystem
  if ($stats['name'] != "ipsum.txt") {
    unset($stats['mtime']);
  }

  return $stats;
}

test("ziparchive_001.zip");
