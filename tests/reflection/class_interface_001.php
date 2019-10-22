<?php
namespace reflection\class_interface_001;

interface TestInterface
{
}

class TestClass1 implements TestInterface
{
}

class TestClass2
{
}

$tmp1 = new \ReflectionClass(TestClass1::class);
$tmp2 = new \ReflectionClass(TestClass2::class);

echo $tmp1->implementsInterface(TestInterface::class) ? "true" : "false";
echo $tmp2->implementsInterface(TestInterface::class) ? "true" : "false";