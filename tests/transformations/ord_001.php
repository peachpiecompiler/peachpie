<?php
namespace transformations\ord_001;

function test_string_int(string $s, int $i) {
  return ord($s[$i]);
}

function test_string_any(string $s, $i) {
  return ord($s[$i]);
}

function test_phpstring_int(string $s, int $i) {
  $ws = hex2bin(bin2hex($s));
  return ord($ws[$i]);
}

function test_phpstring_any(string $s, $i) {
  $ws = hex2bin(bin2hex($s));
  return ord($ws[$i]);
}

function test_any_any($s, $i) {
  return ord($s[$i]);
}

echo test_string_int("foo", 1) ."\n";
echo test_string_int("foo", 666) ."\n";
echo test_string_any("foo", 1) ."\n";
echo test_string_any("foo", 666) ."\n";
echo test_phpstring_int("foo", 1) ."\n";
echo test_phpstring_int("foo", 666) ."\n";
echo test_phpstring_any("foo", 1) ."\n";
echo test_phpstring_any("foo", 666) ."\n";
echo test_any_any("foo", 1) ."\n";
echo test_any_any("foo", 666) ."\n";
