<?php
namespace strings\echo_002; 

/** statically declared, called directly */
function a() {
    return false;
}

if(!function_exists('f')) {
    /**
     * dynamically declared function,
     * will be called with callsites
     */
    function f() {
        return false;
    }
}

echo "A", a(), f(), "B";
