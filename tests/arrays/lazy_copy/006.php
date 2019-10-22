<?php
namespace arrays\lazy_copy_006;

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

function bar($x, $prefix)
{
    echo "array enumeration using current(), prev(), reset(), end():\n";
    
    $first = reset($x);
    $end = end($x);

    echo "$prefix first: " . $first . "\n";
    echo "$prefix end: " . (is_array($end) ? "{array}" : $end) . "\n";

    while ($val = current($x))
    {
        if (!is_array($val))
            echo $prefix . key($x) .": $val\n";
        else
        {
            bar($val, $prefix . "    ");
        }

        if (key($x) % 2 == 0)
            unset($x[key($x)]);  // lazy copy

        prev($x);
    }

    $x[] = "end";

    
    return $x;
}

function test($x)
{
    return bar($x, null);
}

$arr = array( 1,2,3, array( 4,5,6, array( 7,8,9 ) ) );
print_r( test( $arr ) );
