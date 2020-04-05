<?php

class X {
    static function foo() {}
}

class Y extends X {
    static function __callstatic($name, $args) {} // case-insensitive name
    private static function hidden() {}
}

function test() // non-dynamic class scope
{
    X::bar/*!PHP5009!*/();
    X::foo(); // no warning
    Y::bar(); // no warning
    Y::hidden(); // no warning
}