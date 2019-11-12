<?php
namespace constructs\if_statement;

f();

function f()
{
	$x = 0;
	$y = 1;

	echo "\n-- 1 ---------------\n";

	if ($x) echo "0";
	if ($y) echo "1"; else echo "2";
	if (1) echo "3";
	if (1) echo "4"; else echo "5";
	if (0) echo "6";
	if (0) echo "7"; else echo "8";

	echo "\n-- 2 ---------------\n";

	if ($x) echo "9"; elseif ($y) echo "a";
	if (0) echo "b"; elseif ($y) echo "c";
	if (1) echo "d"; elseif ($y) echo "e";
	if ($x) echo "f"; elseif (1) echo "g";
	if ($x) echo "h"; elseif (0) echo "i";
	if (1) echo "j"; elseif (0) echo "k";
	if (1) echo "l"; elseif (1) echo "m";
	if (0) echo "n"; elseif (1) echo "o";
	if (0) echo "p"; elseif (0) echo "q";

	echo "\n-- 3 ---------------\n";

	if ($x) echo "9"; elseif ($y) echo "a"; else echo "A";
	if (0) echo "b"; elseif ($y) echo "c"; else echo "B";
	if (1) echo "d"; elseif ($y) echo "e"; else echo "C";
	if ($x) echo "f"; elseif (1) echo "g"; else echo "D";
	if ($x) echo "h"; elseif (0) echo "i"; else echo "E";
	if (1) echo "j"; elseif (0) echo "k"; else echo "F";
	if (1) echo "l"; elseif (1) echo "m"; else echo "G";
	if (0) echo "n"; elseif (1) echo "o"; else echo "H";
	if (0) echo "p"; elseif (0) echo "q"; else echo "I";

	echo "\n-- 4 ---------------\n";

	if ($x) echo "9"; elseif ($y) echo "a"; elseif (0) echo "a"; else echo "A";
	if (0) echo "b";  elseif ($y) echo "c"; elseif (1) echo "c"; else echo "B";
	if (1) echo "d";  elseif ($y) echo "e"; elseif (0) echo "e"; else echo "C";
	if ($x) echo "f"; elseif (1)  echo "g"; elseif (1) echo "g"; else echo "D";
	if ($x) echo "h"; elseif (0)  echo "i"; elseif (0) echo "i"; else echo "E";
	if (1) echo "j";  elseif (0)  echo "k"; elseif ($y)  echo "k"; else echo "F";
	if (1) echo "l";  elseif (1)  echo "m"; elseif ($x)  echo "m"; else echo "G";
	if (0) echo "n";  elseif (1)  echo "o"; elseif ($y)  echo "o"; else echo "H";
	if (0) echo "p";  elseif (0)  echo "q"; elseif ($x)  echo "q"; else echo "I";
	
	echo "\n-- 5 ---------------\n";

	if (0) echo "J"; elseif (0) echo "K"; elseif (1) echo "L"; elseif (1) echo "M";
}
