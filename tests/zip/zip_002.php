<?php
namespace zip\zip_002;

function print_ret($ret) {
  if (is_bool($ret)) {
    echo ($ret ? "true\n" : "false\n");
  } else if (is_numeric($ret)) {
    echo $ret ."\n";
  } else if (is_string($ret)) {
    echo "\"$ret\"\n";
  } else if (is_null($ret)) {
    echo "NULL\n";
  } else {
    print_r($ret);
    echo "\n";
  }
}

function test($file) {
  print_ret(@zip_open("nonexisting.zip"));
  print_ret(@zip_entry_read("nonsense"));

  $zip = zip_open($file);
  echo get_resource_type($zip) ."\n";

  $zip_entry = zip_read($zip);
  echo get_resource_type($zip_entry) . "\n";

  zip_close($zip);

  print_ret(@zip_read($zip));
  print_ret(@zip_entry_open($zip, $zip_entry));
  print_ret(@zip_entry_read($zip_entry));

  print_ret(@zip_entry_close($zip_entry));
}

test("archive.zip");
