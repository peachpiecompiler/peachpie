<?php
namespace functions\return_type;

function f(int $x) : ?array
{
    if ($x == 0)
    {
        return null;
    }
    else if ($x == 1)
    {
        $n = null;
        return $n;
    }
    else if ($x == 2)
    {
        $arr = (array)123;
        return $arr;
    }
    else
    {
        $something = $GLOBALS["arr"];   // : mixed // type cannot be determined in ct
        return $something;
    }
}

$arr = null;

print_r( f(0) );
print_r( f(1) );
print_r( f(2) );
print_r( f(3) );

echo "Done.";
