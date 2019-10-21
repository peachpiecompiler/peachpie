<?php
namespace generators\generators_010;

function f($a, $b)
{
    $temp = (($a) ? (($b) ? (yield 1) : (yield 2)) : (($b) ? (yield 3) : (yield 4)));
    echo $temp."\n";
}

$gen = f(true, false);
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send(4)."\n";
echo "s:".$gen->send(8)."\n";

echo "Done.";






