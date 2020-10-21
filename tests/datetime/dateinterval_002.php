<?php
namespace date\dateinterval_002;
use DateTime;

function test() {
  
    // https://github.com/peachpiecompiler/peachpie/issues/843
    $sdate = new DateTime("2015-10-22 21:25:32");
    $after = new DateTime("2020-08-31 21:25:32");
 
    $diff = $after->diff($sdate);

    echo "y:, ", $diff->y, "m: ", $diff->m, PHP_EOL;
}

test();

echo "Done.";
