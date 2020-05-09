<?php
namespace generators\generators_017;

// "yield" in "try" block
// https://github.com/peachpiecompiler/peachpie/issues/604

function g($arr)
{
    echo "Start";

    try {

        foreach ($arr as $x)
        {
            yield "[$x]";
        }

    } catch (\Exception $ex) {
    }

    echo "End";
}

foreach (g([1,2,3]) as $x) echo $x;