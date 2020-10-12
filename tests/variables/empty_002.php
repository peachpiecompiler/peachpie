<?php
namespace variables\empty_002;

function foo() {
    /* Test inside a function */
    if (isset($somethingelse)) {
        $bar2 = array();	// this variable get's optimized out
    }

    echo empty($foo2); // OK
    echo empty($bar2); // NullReferenceException
}

/* Test not inside a function */
if (isset($something)) {
    $bar1 = array();
}

echo empty($foo1); // OK
echo empty($bar1); // OK

foo();

echo "Done.";
