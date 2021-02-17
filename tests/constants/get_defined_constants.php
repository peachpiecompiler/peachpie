<?php
namespace constants\get_defined_constants;

define("MY_USER_CONST1", __FILE__);

function test() {
    $all = \get_defined_constants();
    $all = \get_defined_constants(true);
    $core = (new \ReflectionExtension("Core"))->getConstants();

    if (count($all["Core"]) != count($core)) {
        echo "fail, count mismatch";
    }
    else {
        echo "ok", PHP_EOL;
    }

    if (count($core) == 0) {
        echo "fail, no Core constants", PHP_EOL;
    }

    if (count($all["user"]) == 0) {
        echo "fail, no user constants", PHP_EOL;
    }

    if ($all["user"]["MY_USER_CONST1"] != __FILE__) {
        echo "fail, value mismatch", PHP_EOL;
    }
}

test();

echo "Done.";
