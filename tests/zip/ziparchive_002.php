<?php
namespace zip\ziparchive_002;

function print_ret($ret) {
  if (is_bool($ret)) {
    echo $ret ? "true\n" : "false\n";
  } else {
    echo $ret ."\n";
  }
}

function print_props($zip) {
  echo $zip->status ."|". $zip->statusSys ."|". $zip->numFiles ."|". strtolower($zip->filename) ."|". $zip->comment ."|". @$zip->getStatusString() ."\n";
}

function test($filename) {
  // Delete the archive file possibly hanging there from previous test
  if (file_exists($filename)) {
    unlink($filename);
  }

  $zip = new \ZipArchive();
  print_props($zip);

  // Error - not existing file
  print_ret($zip->open($filename));
  print_props($zip);

  // Creating a new file
  print_ret($zip->open($filename, \ZipArchive::CREATE));
  print_props($zip);

  print_ret($zip->addEmptyDir("foo"));
  print_props($zip);

  // Add an existing entry
  print_ret($zip->addEmptyDir("foo"));
  print_props($zip);

  $zip->close();
  print_props($zip);

  // Overwriting an existing archive with exclusive mode
  $zip2 = new \ZipArchive();
  print_ret($zip2->open($filename, \ZipArchive::EXCL));
  print_props($zip2);

  // Clean up the created archive
  unlink($filename);
}

test("ziparchive_002.zip");
