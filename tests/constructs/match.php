<?php
namespace constructs\matchexpr;

// we have to use eval for PHP < 8.0
if (defined("T_MATCH")) {
    $x = 123;
    eval('echo match($x) {
        10 => 1,
        20 => 2,
        "hello" => 3,
        123 => 4,
        default => 5,
    };');
}
else {
    echo 4;
}

echo "Done.";
