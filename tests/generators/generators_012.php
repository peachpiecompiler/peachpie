<?php
namespace generators\generators_012;

function g($a)
{   
    static $b = 10;
    yield $a + $b;
    $b += 10;
}

$gen = g(1);
foreach($gen as $key => $value){
    echo "k:".$key."v:".$value."\n";
}

echo "----------------------\n";

$gen = g(1);
foreach($gen as $key => $value){
    echo "k:".$key."v:".$value."\n";
}

echo "Done.";
