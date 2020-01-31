<?php
namespace reflection\class_property_002;

class X {
    static $p = 666;
}

// internal reflection should be able to process context-static properties
print_r( get_class_vars( __NAMESPACE__ . "\\X" ) );

echo "Done.";
