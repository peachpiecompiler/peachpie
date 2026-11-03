<?php
namespace date\strtotime_of;

// Fixed base and UTC so the output does not depend on the clock.
// Expected lines are what PHP 8.3 prints for these inputs (gmdate).
function test() {
    date_default_timezone_set('UTC');
    error_reporting(0);

    $base = 1700000000; // 2023-11-14 22:13:20 UTC

    $cases = array(
        '2020-01',
        '2024-02',
        '2024-2',
        '2024-5',
        '1999-12',
        '2024-00',
        '2024-12',
        '0999-12',
        '2024-13',
        '2024-99',
        '2020-13-01',
        '2024-123',
        '2024-365',
        '2024-366',
        '2024-001',
        '2024.123',
        '2024365',
        '2024-02 10:00',
        '2024-02 10:00:30',
        '2024-02T10:00:00',
        '2024-02 +1 day',
        '2024-02 -1 day',
        '2024-02 +1 month',
        '2024-02 first monday',
        '2024-02 -05',
        '2024-02 2024-03',
        '2020-01-01',
        '2024-2-3',
        '2024-02-03',
        'first monday of 2020-01',
        'first monday of january 2020',
        'FIRST MONDAY OF JANUARY 2020',
        'first monday of january 2020 10:00',
        'first monday of january 2020 midnight',
        'first monday of january 2020 +1 day',
        'first  monday   of   january   2020',
        "first\tmonday\tof\tjanuary\t2020",
        'first monday of february 2021',
        'first monday of february 2021 +1 day',
        'last friday of january 2020',
        'last friday of february 2024',
        'second tuesday of march 2020',
        'fourth thursday of november 2020',
        'fifth sunday of february 2020',
        'fifth monday of february 2021',
        'this monday of january 2020',
        'previous monday of january 2020',
        'first monday of next month',
        'first monday of next month 10:00',
        'first monday of +1 month',
        'first monday of this month',
        'last friday of next month',
        'last friday of last month',
        'last friday of last year',
        'last sunday of +1 month',
        'first monday of 2020',
        'last friday of 2020',
        'first monday of',
        'last friday of',
        'first day of',
        'last day of',
        'first day of +1 month',
        'last day of +1 month',
        'last day of +1 month 15:30',
        'last day of +2 hours',
        'first day of +1 day',
        'last day of +1 day',
        'first day of next month',
        'last day of next month',
        'first day of last month',
        'last day of this month',
        'first day of january 2020',
        'last day of january 2020',
        'last day of january 2020 10:00:30',
        'last day of february',
        'last day of february 2020',
        'last day of february 2024',
        'last day of february 1900',
        'last day of 2020',
        'first day of 2020',
        'last day of december 2020',
        'last day of 2024-02',
        'first day of 2024-02',
        'first monday of 2024-02',
        'last day of  february 2020',
        "last day of\tfebruary 2020",
        'last  day of february 2020',
        'last day  of february 2020',
        'first day  of january 2020',
        'february',
        'may',
        't10:00',
        'XII 2020',
        'last day of XII 2020',
        'xii 2020',
        'last day of xii 2020',
        'of',
        'OF',
        'Of',
        'monday of',
        'first of january 2020',
        'last of january 2020',
        'second day of january 2020',
        'the first monday of january 2020',
        'last may',
        'first january',
        'foobar',
        '01-02',
        '12-31',
        '2020-01-01 2020-02-02',
        '2020-01-01 garbage',
        'now',
        '+1 day',
        'next monday',
        '2024W05',
        '2020-01-01 10:00',
    );

    foreach ($cases as $input) {
        $result = strtotime($input, $base);
        if ($result === false) {
            echo $input, " => false", PHP_EOL;
        } else {
            echo $input, " => ", gmdate('Y-m-d H:i:s', $result), PHP_EOL;
        }
    }

    // Years before 1901. 0999 is a real 4-digit year (not 1999).
    // 1900 is not a Gregorian leap year, so February ends on the 28th.
    // Both are outside the old 32-bit Unix range; the values below are UTC.
    foreach (array('0999-12', 'last day of february 1900') as $input) {
        $result = strtotime($input, $base);
        echo "ts ", $input, " => ", $result, PHP_EOL;
        echo "c ", $input, " => ", date('c', $result), PHP_EOL;
    }

    echo "Done.", PHP_EOL;
}

test();
