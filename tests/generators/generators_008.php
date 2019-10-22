<?php
namespace generators\generators_008;

function bar(){
    static $s = 0;
    echo "curr_s:".$s."\n";
    return ++$s;
}

function f()
{
    $var = (yield 10)  + (yield 20);
    echo "var:".$var."\n";

    $var = bar() + (yield 10) + bar()  + (yield 20) + bar();
    echo "var:".$var."\n";
    
    $var = (yield (yield (yield 30)));
    echo "var:".$var."\n";

    $var = (yield bar() + (yield bar() + (yield 30)));
    echo "var:".$var."\n";

}

$gen = f();
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send(100)."\n";
echo "s:".$gen->send(200)."\n";

echo "s:".$gen->send(100)."\n";
echo "s:".$gen->send(200)."\n";

echo "s:".$gen->send(100)."\n";
echo "s:".$gen->send(200)."\n";
echo "s:".$gen->send(400)."\n";

echo "s:".$gen->send(100)."\n";
echo "s:".$gen->send(100)."\n";
echo "s:".$gen->send(400)."\n";

echo "Done.";


