<?php
namespace generators\generators_003;

function f($a)
{   
    $b = 100;
    yield $a;
    $a += 5; 
    yield $a + 10;
    $b += 1000;
    yield $a + $b + 10;
}

$gen = f(1);
foreach($gen as $key => $value){
    echo "k:".$key."v:".$value."\n";
}

echo "r:".$gen->getReturn()."\n";
echo "k:".$gen->key()."v:".$gen->current()."c:".$gen->valid()."\n";

echo "-------------------------------------------------\n";

$a = 1;
$b = 2;

function g()
{
    global $a, $b;
    yield $a + $b;
    $a = 5;
}

echo "a".$a."\n";
foreach(g() as $key => $value){
    echo "k:".$key."v:".$value."\n";
}
echo "a".$a."\n";

echo "Done.";




