<?php
namespace classes\new_003;

class X
{
    function __toString() { throw new Exception('unreachable'); } // <-- new should not call 
}

$a = new X;
$b = new $a; // indirect new

print_r( get_class($b) ); // __namespace\X

echo "Done.";
