<?php
namespace zip\ziparchive_003;

// https://stackoverflow.com/a/8688278/2105235
function delete_dir($path) {
    if (empty($path)) { 
        return false;
    }
    return is_file($path) ?
            @unlink($path) :
            array_map(__FUNCTION__, glob($path.'/*')) == @rmdir($path);
}

function test($filename, $dir) {
  // Create temporary copy to prevent any changes to the original archive
  $tmp_filename = $filename .".hlp";
  copy($filename, $tmp_filename);

  // Create the archive
  $zip = new \ZipArchive();
  if ($zip->open($tmp_filename)!==TRUE) {
    exit("cannot open <$filename>\n");
  }

  // Extract, check and delete
  mkdir($dir);
  $zip->extractTo($dir);
  print_r(scandir($dir));
  print_r(scandir($dir ."/archive"));
  delete_dir($dir);

  $zip->close();
  unlink($tmp_filename);
}

test("archive.zip", "tmp");
