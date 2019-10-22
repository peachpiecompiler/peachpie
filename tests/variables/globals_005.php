<?php
namespace variables\globals_005;

$g1 = 1;
$g2 = 2;
$g3 = 3;
$g4 = 4;

function foo($gname)
{
	global $g1, $$gname;
	$g1 --;
	$$gname = 666;
}

foo("g2");
print_r($g1);
print_r($g2);

foo("g3");
print_r($g1);
print_r($g3);
