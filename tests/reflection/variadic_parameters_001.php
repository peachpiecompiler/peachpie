<?php
namespace reflection\variadic_parameters_001;

function func1(...$a)
{
}

function func2($a)
{
}

$tmp1 = new \ReflectionFunction(__NAMESPACE__ . "\\func1");
$tmp2 = new \ReflectionFunction(__NAMESPACE__ . "\\func2");
$variadic1 = $tmp1->getParameters()[0]->isVariadic();
$variadic2 = $tmp2->getParameters()[0]->isVariadic();

echo $variadic1 ? "true" : "false";
echo $variadic2 ? "true" : "false";