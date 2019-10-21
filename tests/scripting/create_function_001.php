<?php
namespace scripting\create_function_001;

function test()
{
    $fname = create_function('$a, $b', 'echo __FUNCTION__; return $a + $b;');
    $r = $fname(10, 11);
    echo $r;
}

test();
