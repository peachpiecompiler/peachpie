<?php
namespace reflection\variadic_parameters_003;

class X003
{
    public function foo(string $a, $b = [1,2,3])
    {
        return func_get_args(); // causes default parameters to be treated as `params PhpValue[]`, creates "fake" parameters
    }
}

$method = new \ReflectionMethod(X003::class, "foo");
print_r($method->getParameters());
