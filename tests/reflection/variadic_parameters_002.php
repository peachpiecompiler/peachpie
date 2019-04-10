<?php

function func1(...$a)
{
}

function func2($a)
{
}

$tmp1 = new ReflectionFunction("func1");
$tmp2 = new ReflectionFunction("func2");

echo $tmp1->isVariadic() ? "true" : "false";
echo $tmp2->isVariadic() ? "true" : "false";