<?php
namespace operators\assign_004;

class X
{
    static function &a()
    {
        $a = 666;
        return $a;
    }
}

function foo()
{
    global $a;

    $a = X::a(); // value must be dereferenced (and copied)

    echo $a;
}

foo();
