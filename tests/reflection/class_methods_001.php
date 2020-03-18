<?php
namespace reflection\class_methods_001;

class myclass {
    // constructor
    function __construct(){}

    // method 1
    function myfunc1(){}

    // method 2
    function myfunc2(){}
}

class C
{
    private function privateMethod(){}
    public function publicMethod(){}
    public function __construct()
    {
        print_r(in_array('privateMethod', get_class_methods(__NAMESPACE__.'\C')));
		print_r(method_exists($this, 'privateMethod'));
    }
}

print_r(in_array('__construct', get_class_methods(__NAMESPACE__.'\myclass')));
print_r(in_array('myfunc2', get_class_methods(new myclass)));
print_r(in_array('privateMethod', get_class_methods(new C)));
print_r(method_exists(__NAMESPACE__.'\C', 'privateMethod'));