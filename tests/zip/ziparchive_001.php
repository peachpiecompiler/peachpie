<?php

function test($filename) {
  // Delete the archive file possibly hanging there from previous test
  if (file_exists($filename)) {
    unlink($filename);
  }

  // Create the archive
  $zip = new ZipArchive();
  if ($zip->open($filename, ZipArchive::CREATE)!==TRUE) {
    exit("cannot open <$filename>\n");
  }

  // Fill the archive
  $zip->addFromString("testfilephp.txt", "#1 This is a test string added as testfilephp.txt.\n");
  $zip->addFromString("testfilephp2.txt", "#2 This is a test string added as testfilephp2.txt.\n");
  $zip->addFile("ziparchive_001.txt","ipsum.txt");
  echo "filename: ". $zip->filename ."\n";
  echo "numfiles: " . $zip->numFiles . "\n";
  //echo "status:" . $zip->status . "\n";
  $zip->close();

  // Open the archive again and list its contents
  $zip_res = zip_open($filename);
  while ($zip_entry = zip_read($zip_res)) {
    $filesize = zip_entry_filesize($zip_entry);
    echo zip_entry_name($zip_entry) .'|'. $filesize ."\n";
    zip_entry_open($zip_res, $zip_entry, "r");
    echo zip_entry_read($zip_entry, $filesize) ."\n";
    zip_entry_close($zip_entry);
  }
  zip_close($zip_res);

  // Clean up the created archive
  unlink($filename);
}

test("ziparchive_001.zip");
