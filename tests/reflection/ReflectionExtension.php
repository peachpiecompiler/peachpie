<?php

namespace reflection\reflectionextension;

use ReflectionExtension;

function test() {

    $re = new ReflectionExtension("pcre");
    echo $re->getName() . PHP_EOL;
    echo count( $re->getClasses() ), PHP_EOL; // 0
    
    $constants = $re->getConstants();
    echo $constants['PCRE_VERSION'] == PCRE_VERSION ? "ok" : "fail", PHP_EOL; // ok
}

test();
