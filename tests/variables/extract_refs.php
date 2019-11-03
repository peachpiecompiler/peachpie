<?php
namespace variables\extract_refs;

function foo(&$x)
{
    extract(array("x" => 123));
}

$x = 0;

foo($x);

echo $x;
