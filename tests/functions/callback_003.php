<?php
namespace functions\callback_003;

class BaseClass
{
    private $var = 10;

    public function testFunc()
    {
        return $this->var;
    }
}

class TestClass extends BaseClass
{
    public function testFunc()
    {
        return call_user_func_array("parent::testFunc", []);
    }
}

echo (new TestClass())->testFunc(), PHP_EOL;

echo "Done.";