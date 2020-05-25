<?php
namespace generators\generators_018;

// "yield" in "catch/finally" block
// https://github.com/peachpiecompiler/peachpie/issues/604

function g($a)
{
    echo "Start,";

    try {

        yield "0,";

        if ($a) throw new \Exception;

    } catch (\Exception $ex) {

        echo "<Catch,";

        yield "1,";
        yield "2,";
        
        echo "Catch>,";
    }
    finally {
        echo "Finally,";
    }

    echo "End,";
}

foreach (g(true) as $x) echo $x;
echo PHP_EOL;
foreach (g(false) as $x) echo $x;
echo PHP_EOL, "Done.";