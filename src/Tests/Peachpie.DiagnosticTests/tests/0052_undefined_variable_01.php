<?php

function bar($foo) {
    $alwaysDefined = 5;

    echo $alwaysUndefined/*!PHP0052!*/;

    if ($foo > 0) {
        $maybeUndefined = 0;
    }
    echo $maybeUndefined/*!PHP0052!*/;

    echo $alwaysDefined;
}

bar(3);
