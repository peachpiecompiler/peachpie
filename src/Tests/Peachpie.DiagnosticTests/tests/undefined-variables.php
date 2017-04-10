<?php
// Examples of diagnostics of undefined (uninitialized) variables

// Defined variables and ensured accesses - diagnostics won't appear

$foo = 42;
echo $foo;

$ref = &$ensuredRef;
echo $ensuredRef;

$ensuredArray[] = 0;

// Possibly undefined variables - diagnostics will appear upon their usage

function bar($foo) {
    echo $alwaysUndefined/*!PHP0052!*/;

    if ($foo > 0) {
        $maybeUndefined = 0;
    }
    echo $maybeUndefined/*!PHP0052!*/;
}

bar(3);
