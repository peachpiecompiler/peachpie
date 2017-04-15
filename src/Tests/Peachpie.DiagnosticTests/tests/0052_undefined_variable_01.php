<?php

function bar($foo) {
    $alwaysDefined = 5;

    echo $alwaysUndefined/*!PHP3007!*/;

    if ($foo > 0) {
        $maybeUndefined = 0;
    }
    echo $maybeUndefined/*!PHP3007!*/;

    echo $alwaysDefined;
}

bar(3);
