<?php
namespace classes\exceptions_001;

function test() {
    try {
        echo "foo";
        throw new \InvalidArgumentException("Lorem");
    } catch (\Throwable $e) {
        echo $e->getMessage();
    }
}

test();
