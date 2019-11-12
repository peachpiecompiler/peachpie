<?php
namespace zip\zip_001;

function enumerate($file) {
  $zip = zip_open($file);

  if ($zip) {
    while ($zip_entry = zip_read($zip))  {
      echo "Name:               " . zip_entry_name($zip_entry) . "\n";
      echo "Actual Filesize:    " . zip_entry_filesize($zip_entry) . "\n";
      echo "Compressed Size:    " . zip_entry_compressedsize($zip_entry) . "\n";
      //echo "Compression Method: " . zip_entry_compressionmethod($zip_entry) . "\n";

      if (zip_entry_open($zip, $zip_entry, "r")) {
        echo "File Contents:\n";
        $buf = zip_entry_read($zip_entry, zip_entry_filesize($zip_entry));
        echo "$buf\n";

        zip_entry_close($zip_entry);
      }

      echo "\n";
    }

    zip_close($zip);
  }
}

enumerate("archive.zip");
