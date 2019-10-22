<?php
namespace strings\json_001;

function test($obj, $opt = 0)
{
	print_r($obj);
	$x = json_encode($obj, $opt);
	echo "\n$x\n";
	print_r( json_decode($x, true) );
	echo "\n";
}

test("special characters \" / # @ < > & ' \" ");
test(array(1,2,3,4,array(1,2,'x'=>'y')));
test(array ('a'=>1,'b'=>2,'c'=>3,'d'=>4,'e'=>5));

$a = array('<foo>',"'bar'",'"baz"','&blong&');

echo "Normal: ", json_encode($a), "\n";
echo "Tags: ",   json_encode($a,JSON_HEX_TAG), "\n";
echo "Apos: ",   json_encode($a,JSON_HEX_APOS), "\n";
echo "Quot: ",   json_encode($a,JSON_HEX_QUOT), "\n";
echo "Amp: ",    json_encode($a,JSON_HEX_AMP), "\n";
echo "All: ",    json_encode($a,JSON_HEX_TAG|JSON_HEX_APOS|JSON_HEX_QUOT|JSON_HEX_AMP), "\n\n";

$b = array();

echo "Empty array output as array: ", json_encode($b), "\n";
echo "Empty array output as object: ", json_encode($b, JSON_FORCE_OBJECT), "\n\n";

$c = array(array(1,2,3));

echo "Non-associative array output as array: ", json_encode($c), "\n";
echo "Non-associative array output as object: ", json_encode($c, JSON_FORCE_OBJECT);

echo "Done.";
