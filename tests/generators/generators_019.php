<?php
namespace generators\generators_019;

function g() { // generator that gets its yield optimized out in release
    if (false) {
        yield true;
    }
}

foreach (g() as $value) {
    echo $value;
}

echo "Done.";
