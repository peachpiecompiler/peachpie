<?php
namespace variables\indirect_003;

// Set variable and read it via indirect access
$a = "a";
echo $$a."<br>";
// Take reference of $GLOBALS
$a = array(1=>"hello", "two"=>"world");
$b = "c";
$$b =& $a;
echo $c[1]." ".$c["two"]."<br>";
// Set $$a
$a = "b";
$$a = "Set via indirect variable";
echo $b;
