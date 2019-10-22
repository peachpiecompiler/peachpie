<?php
namespace strings\echo_001;
echo "Hello World";

echo "This spans
multiple lines. The newlines will be 
output as well";

echo "This spans\nmultiple lines. The newlines will be\noutput as well.";

echo "Escaping characters is done \"Like this\".";

// You can use variables inside of an echo statement
$foo = "foobar";
$bar = "barbaz";

echo "foo is $foo"; // foo is foobar

// Using single quotes will print the variable name, not the value
echo 'foo is $foo'; // foo is $foo

// If you are not using any other characters, you can just echo variables
echo $foo;          // foobar
echo $foo,$bar;     // foobarbarbaz

// You can also use arrays
$bar = array("value" => "foo");

echo "this is {$bar['value']} !"; // this is foo !

// Some people prefer passing multiple parameters to echo over concatenation.
echo 'This ', 'string ', 'was ', 'made ', 'with multiple parameters.', chr(10);
echo 'This ' . 'string ' . 'was ' . 'made ' . 'with concatenation.' . "\n";

$variable = "[VARIABLE]";

$some_var = 1;
// Because echo is not a function, following code is invalid. 
//REM ($some_var) ? echo 'true' : echo 'false';

// However, the following examples will work:
($some_var) ? print('true'): print('false'); // print is a function
echo $some_var ? 'true': 'false'; // changing the statement around
?>

SHORT TAG SYNTAX: <?=$some_var?>
