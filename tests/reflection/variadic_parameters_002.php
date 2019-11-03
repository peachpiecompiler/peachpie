<?php
namespace reflection\variadic_parameters_002;

function func1(...$a)
{
}

function func2($a)
{
}

$tmp1 = new \ReflectionFunction(__NAMESPACE__ . "\\func1");
$tmp2 = new \ReflectionFunction(__NAMESPACE__ . "\\func2");

echo $tmp1->isVariadic() ? "true" : "false";
echo $tmp2->isVariadic() ? "true" : "false";