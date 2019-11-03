<?php
namespace strings\str_word_count;

$str = "Hello friend, you're looking          good today!";

$str2 = "F0o B4r 1s bar foo";

$str3 = "foo'0 bar-0var bar-xvar bar- var'";

print_r(str_word_count($str, 1));
print_r(str_word_count($str, 2));
echo str_word_count($str),"\n";

echo str_word_count($str2, NULL, "04"),"\n";
echo str_word_count($str2, NULL, "01"),"\n";
echo str_word_count($str2, NULL, "014"),"\n";
echo str_word_count($str2, NULL, ""),"\n";

echo "-0-\n";

print_r(str_word_count($str2, 1, "04"));

echo "-1-\n";

print_r(str_word_count($str2, 1, "01"));

echo "-2-\n";

print_r(str_word_count($str2, 1, "014"));

echo "-3-\n";

print_r(str_word_count($str2, 1, ""));

echo "-4-\n";

print_r(str_word_count($str2, 2, "04"));

echo "-5-\n";

print_r(str_word_count($str2, 2, "01"));

echo "-6-\n";

print_r(str_word_count($str2, 2, "014"));

echo "-7-\n";

print_r(str_word_count($str2, 2, ""));

echo "-8-\n";

print_r(str_word_count($str3, 2, "0"));
