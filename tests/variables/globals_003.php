<?php
namespace variables\globals_003;
// Set variable and read it via $GLOBALS
$a = 5;
echo $GLOBALS["a"]."<br>";
// Take reference of $GLOBALS
$a = array(1=>"hello", "two"=>"world");
$b =& $GLOBALS;
echo $b["a"][1]." ".$b["a"]["two"]."<br>";
// Set $GLOBALS
$GLOBALS["a"] = "Set via GLOBALS"."<br>";
echo $a;
