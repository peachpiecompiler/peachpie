<?php
namespace reflection\class_constant_001;

class TestClass1
{
    const CONSTANT1 = 1;
    const CONSTANT2 = 2;
}

class TestClass2
{
    const CONSTANT2 = 2;
}

$tmp1 = new \ReflectionClass(TestClass1::class);
$tmp2 = new \ReflectionClass(TestClass2::class);

echo $tmp1->hasConstant("CONSTANT1") ? "true" : "false";
echo $tmp1->hasConstant("CONSTANT2") ? "true" : "false";
echo $tmp1->hasConstant("constant1") ? "true" : "false";
echo $tmp1->hasConstant("constant2") ? "true" : "false";
echo $tmp2->hasConstant("CONSTANT1") ? "true" : "false";
echo $tmp2->hasConstant("CONSTANT2") ? "true" : "false";
echo $tmp2->hasConstant("constant1") ? "true" : "false";
echo $tmp2->hasConstant("constant2") ? "true" : "false";