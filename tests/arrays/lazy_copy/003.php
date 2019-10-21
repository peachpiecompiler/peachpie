<?php
namespace arrays\lazy_copy_003;

// causes lazy copy:
function foo($x)
{
    echo "keyed aliased foreach enumeration:";
    foreach ($x as &$v)
    {
        if (!is_array($v))
        {
            echo "- $v\n";
            $v = "dummy$v";
        }        
    }

    return $x;
}

function bar($x)
{
    echo "array enumeration using current(), next(), reset(), end():";

    while ($val = current($x))
    {
        if (!is_array($val))
            echo "- $val\n";
        next($x);
    }

    $x[] = "end";// ensure the end is not array, and perform lazy copy

    echo "first: " . reset($x);
    echo "end: " . end($x);

    return $x;
}

function test($x)
{
    $x = foo($x);
    $x = bar($x);

    return $x;
}

$arr = array( 1,2,3, array( 4,5,6, array( 7,8,9 ) ) );
test( $arr );
print_r( $arr );