<?php
namespace date\strtotime_of;

function test() {
    // Sanity: basic parsing that should always work
    echo "S1. now: ", date("Y-m-d H:i:s", strtotime("now")), PHP_EOL;
    echo "S2. today: ", date("Y-m-d H:i:s", strtotime("today")), PHP_EOL;
    echo "S3. +1 day: ", date("Y-m-d H:i:s", strtotime("+1 day")), PHP_EOL;

    // PHP documentation examples for the "of" keyword in strtotime

    // YEAR_MONTH (YYYY-MM) format
    echo "1. 2020-01: ", date("Y-m-d H:i:s", strtotime("2020-01")), PHP_EOL;

    // first [weekday] of [year-month]
    echo "2. first monday of 2020-01: ", date("Y-m-d H:i:s", strtotime("first monday of 2020-01")), PHP_EOL;

    // first [weekday] of [monthname] [year]
    echo "3. first monday of january 2020: ", date("Y-m-d H:i:s", strtotime("first monday of january 2020")), PHP_EOL;

    // last day of [monthname] [year]
    echo "4. last day of january 2020: ", date("Y-m-d H:i:s", strtotime("last day of january 2020")), PHP_EOL;

    // last day of [year]
    echo "5. last day of 2020: ", date("Y-m-d H:i:s", strtotime("last day of 2020")), PHP_EOL;

    // first of [monthname] [year] (first day of month)
    echo "6. first of january 2020: ", date("Y-m-d H:i:s", strtotime("first of january 2020")), PHP_EOL;

    // last of [monthname] [year] (last day of month)
    echo "7. last of january 2020: ", date("Y-m-d H:i:s", strtotime("last of january 2020")), PHP_EOL;

    // last [weekday] of [monthname] [year]
    echo "8. last friday of january 2020: ", date("Y-m-d H:i:s", strtotime("last friday of january 2020")), PHP_EOL;

    // first [weekday] of [year]
    echo "9. first monday of 2020: ", date("Y-m-d H:i:s", strtotime("first monday of 2020")), PHP_EOL;

    // last [weekday] of [year]
    echo "10. last friday of 2020: ", date("Y-m-d H:i:s", strtotime("last friday of 2020")), PHP_EOL;

    echo "Done." . PHP_EOL;
}

test();
