<?php
namespace transformations\ord_001;

class A {
  public function __toString() { return "A"; }
}

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

function test_latestring_int($s, int $i) {
  $s = (string)$s;
  return ord($s[$i]);
}

function test_any_int($s, int $i) {
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
echo test_latestring_int("foo", 1) ."\n";
echo test_latestring_int("foo", 666) ."\n";
echo test_latestring_int([], 1) ."\n";
echo test_any_int("foo", 1) ."\n";
echo test_any_int("foo", 666) ."\n";
echo test_any_int([], 1) ."\n";
echo test_any_int(['foo', 'bar', 'baz'], 1) ."\n";
echo test_any_int([new A], 0);
echo test_any_int([42], 1);
