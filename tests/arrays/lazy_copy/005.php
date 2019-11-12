<?php
namespace arrays\lazy_copy_005;

// causes lazy copy:
function foo($x)
{
    echo "keyed foreach enumeration:\n";
    foreach ($x as $k => $v)
    {
        if (!is_array($v))
        {
            echo "$k => $v\n";
        }        
    }

    return $x;
}

function bar($x)
{
    echo "array enumeration using current(), next(), reset(), end():\n";
    
    while ($val = current($x))
    {
        if (!is_array($val))
            echo key($x) .": $val\n";
        else
        {
            bar(foo($val));
        }

        unset($x[key($x)]);  // lazy copy

        next($x);
    }

    $x[] = "end";// ensure the end is not array, and perform lazy copy

    echo "first: " . reset($x) . "\n";
    echo "end: " . end($x) . "\n";

    return $x;
}

function test($x)
{
    return bar($x);
}

$arr = array( 1,2,3, array( 4,5,6, array( 7,8,9 ) ) );
print_r( test( $arr ) );
