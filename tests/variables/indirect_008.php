<?php
namespace variables\indirect_008;

// bug https://github.com/peachpiecompiler/peachpie/issues/1002

function test($name) {
    $$name = '1'; // marks all (so far visited) local variables as mixed

    if ($x) { } // CFG: reads $x, creates variable record, but leaves it as "void"

    $y = 1; // CFG: remembers type for next records which effectively creates "void" for $x

    // branching causes the incorrectly created "void" to be changed to "null"
    if (rand()) { // anything
        echo 1;
    }
    else {
        echo 1;
    }

    // type inference treats $x as null and lowers "is_numeric($x)" to "false"
    if (is_numeric($x)) {
        echo "numeric"; // won't get called, the condition is optimized out
    }
}

test("x");
