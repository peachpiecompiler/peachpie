<?php
namespace date\datetime_001;

function test() {
    $dt = new \DateTime('2021-03-06 17:19:12 -5:00');
    $obj = json_decode(json_encode($dt));

    // timezone_type: 1, timezone: -05:00
    echo "timezone_type: ", $obj->timezone_type, ", timezone: ", $obj->timezone, PHP_EOL;
}

test();

echo "Done.";
