<?php
namespace reflection\ReflectionFunction_001;

function foo($a, $b = 1) {
    print_r( func_get_args() ); // causes additional synthesized overloads
}

print_r( basename( (new \ReflectionFunction(__NAMESPACE__ . "\\foo"))->getFileName() ) );

echo "\nDone.";
