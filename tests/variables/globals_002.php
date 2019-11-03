<?php
namespace variables\globals_002;

$a = array(1=>"hello", "two"=>"world");
$GLOBALS["b"] = $a;
echo $GLOBALS["b"][1]." ".$GLOBALS["b"]["two"];
