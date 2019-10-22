<?php
namespace strings\str_replace;

function da($a)
{
  foreach($a as $k => $v) echo "$k => $v\n";
}

// 8 combinations of possible str_replace arguments:

$arr1 = array(
"a" => "hello",
"b" => "world",
"c" => "earth",
"d" => "europe",
"e" => "africa",
"f" => "america",
"g" => "asia",
"h" => "eheheheheheijijijijijlololololo",
"i" => "klpofklpofklpof",
"j" => "www",
"k" => "lo",
"l" => "as",
"m" => "ing",
"n" => "y",
"o" => "string");

$arr2 = array(
"h" => "eheheheheheijijijijijlololololo",
"i" => "klpofklpofklpof",
"j" => "www",
"k" => "lo",
"l" => "as",
"m" => "ing",
"n" => "y");

$str = "hello, very long string with several substrings to be replaced or removed";

echo str_replace(array("hello", "string", "very", "long"), array("XXX"), $str), "\n";// replace only the first needle with XXX
echo str_replace(array("hello", "string", "very", "long"), "XXX", $str), "\n";  // replace all the needles with the same replacement

echo str_replace("","eh, nothing will happen",$str),"\n";
echo str_replace("ing","",$str),"\n";
//echo str_replace("remo",$arr1,$str),"\n";
echo str_replace($arr2,$arr1,$str),"\n";
echo str_replace($arr1,$arr2,$str),"\n";
echo str_replace($arr1,"some replacement",$str),"\n";

da( str_replace("","eh, nothing will happen",$arr1) );
da( str_replace("ing","",$arr2) );
//da( str_replace("remo",$arr1,$arr1) );
da( str_replace($arr2,$arr1,$arr2) );
da( str_replace($arr1,$arr2,$arr1) );
da( str_replace($arr1,"some replacement",$arr2) );
