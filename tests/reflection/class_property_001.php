<?php
namespace reflection\class_property_001;

class TestClass1
{
    /** @var string */
    public $prop1;

    /** @var int */
    public $prop2;
}

class TestClass2
{
    /** @var int */
    public $prop2;
}

$tmp1 = new \ReflectionClass(TestClass1::class);
$tmp2 = new \ReflectionClass(TestClass2::class);

echo $tmp1->hasProperty("prop1") ? "true" : "false";
echo $tmp1->hasProperty("prop2") ? "true" : "false";
echo $tmp2->hasProperty("prop1") ? "true" : "false";
echo $tmp2->hasProperty("prop2") ? "true" : "false";