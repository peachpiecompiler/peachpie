<?php
namespace functions\random_int;

// min/max
function test(int $min, int $max) {
    for ($i = 0; $i < 1000; $i++) {
        $x = \random_int($min, $max);
        if ($x < $min || $x > $max) echo "error: $min < $x < $max";
    }
}

test(0, PHP_INT_MAX);
test(PHP_INT_MIN, 0);
test(PHP_INT_MIN, PHP_INT_MAX);
test(0, 0);
test(0, 1);
test(PHP_INT_MIN, PHP_INT_MIN + 1);

echo "Done.";
