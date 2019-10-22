<?php
namespace operators\assign_002;

function test() {
    $a = $b = '';
    
    $a .= 'A';
    $b .= 'B';
    
    echo $b;
}

test();

echo "\nDone.";
