<?php
namespace reflection\class_instance_001;

class TestClass1
{
}

class TestClass2
{
}

$tmp1 = new \ReflectionClass(TestClass1::class);
$tmp2 = new \ReflectionClass(TestClass2::class);

echo $tmp1->isInstance(new TestClass1()) ? "true" : "false";
echo $tmp1->isInstance(new TestClass2()) ? "true" : "false";
echo $tmp2->isInstance(new TestClass1()) ? "true" : "false";
echo $tmp2->isInstance(new TestClass2()) ? "true" : "false";