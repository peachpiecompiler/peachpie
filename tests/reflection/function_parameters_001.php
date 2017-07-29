<?php

function foo($a, $b, $c = null) { }

class A
{
    public function __construct($lorem, $ipsum = 3) { }

    public function bar($lorem) {
        echo "peachpie";
    }
}

function print_params($params) {
    echo "| ";
    foreach ($params as $param) {
        echo $param->name;
        echo ($param->isOptional()) ? '?' : '!';
        echo " ";
    }
}

$explode = new ReflectionFunction('explode');
print_params($explode->getParameters());

$foo = new ReflectionFunction('foo');
print_params($foo->getParameters());

$a = new ReflectionClass('A');
print_params($a->getConstructor()->getParameters());
print_params($a->getMethod('bar')->getParameters());
