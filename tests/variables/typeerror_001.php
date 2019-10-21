<?php
namespace variables\typeerror_001;

function takeint(int $a) {
    echo $a, PHP_EOL;
}

function take($a) {
    try {
        takeint($a);    // https://github.com/peachpiecompiler/peachpie/issues/490
    }
    catch (\TypeError $err) {
        echo "type error!", PHP_EOL;
    }
}

take(666.66);   // ok
take([]);       // \TypeError
take("123.456");// ok
take("text");   // \TypeError

echo "Done.";
