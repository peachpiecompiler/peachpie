<?php
namespace transformations\conditional_001;

function test_any($a, $i) {
  return isset($a[$i]) ? $a[$i] : 'default';
}

function test_array(array $a, $i) {
  return isset($a[$i]) ? $a[$i] : 'default';
}

function test_string(string $s, $i) {
  return isset($s[$i]) ? $s[$i] : 'default';
}

function test_string_int(string $s, int $i) {
  return isset($s[$i]) ? $s[$i] : 'default';
}


echo test_any(['foo' => 'bar'], 'foo');
echo test_array(['foo' => 'bar'], 'foo');
echo test_string('foo', 1);
echo test_string_int('foo', 1);
