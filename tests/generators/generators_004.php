<?php
namespace generators\generators_004;
function gnr() {
    $value = 5;

    while ($value > 0) {
        $value--;
        yield $value;
    }
}

foreach (gnr() as $val) {
    echo (--$val).' ';
}

echo "\n";

function &gr() {
    $value = 5;

    while ($value > 0) {
        $value--;
        yield $value;
    }
}

foreach (gr() as &$val2) {
    echo (--$val2).' ';
}

echo "Done.";

