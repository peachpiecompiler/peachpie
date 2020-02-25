<?php
namespace variables\isset_005;

function test($name)
{
    $obj = (object)[$name => null, ];
    echo "isset: ", isset( $obj->$name ) ? "1" : "0", PHP_EOL;
    
    $obj = (object)[$name => true, ];
    echo "isset: ", isset( $obj->$name ) ? "1" : "0", PHP_EOL;
}

test("xxx");

echo "Done.";
