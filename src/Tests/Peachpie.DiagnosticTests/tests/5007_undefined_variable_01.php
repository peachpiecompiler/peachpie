<?php

function bar($foo) {
    $alwaysDefined = 5;

    echo $alwaysUndefined/*!PHP5007!*/;

    if ($foo > 0) {
        $maybeUndefined = 0;
    }
    echo $maybeUndefined/* non strict !PHP5007 */;

    echo $alwaysDefined;
}

bar(3);
