<?php
namespace strings\substr_operations;

$var = "aaaaaaaa";

print_r(substr_replace($var, 'b', 0));
print_r(substr_replace($var, 'b', 0, strlen($var)));
print_r(substr_replace($var, 'b', 0, 0));
print_r(substr_replace($var, 'b', 10, -1));
print_r(substr_replace($var, 'b', -7, -1));
print_r(substr_replace($var, 'b', 10, -1));

echo "\n";

print_r(substr_count($var, 'a', 0));
print_r(substr_count($var, 'a', 0, strlen($var)));

print_r(@substr_count($var, null, 0, 0));
print_r(@substr_count($var, '', 0, 0));
print_r(@substr_count($var, 'a', -1, -1));
print_r(substr_count($var, 'a', 3, 0));	// since PHP 7.1, zero-length is allowed
print_r(@substr_count($var, 'a', 10, -1));
print_r(@substr_count($var, 'a', 6, 6));

echo "\n";

print_r(substr_replace(array("a" => $var,10 => $var,"b" => null, "", " "), 'b', -10, 5));

echo "\n";

print_r(substr_compare("abcde", "bc", 1, 2)); // 0
print_r(substr_compare("abcde", "bcg", 1, 2)); // 0
print_r(substr_compare("abcde", "BC", 1, 2, true)); // 0
print_r(substr_compare("abcde", "bc", 1, 3)); // 1
print_r(substr_compare("abcde", "cd", 1, 2)); // -1
