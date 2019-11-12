<?php
namespace generators\generators_011;

function f($a, $b)
{
    $temp = ($a) ? 1 : yield 2;
    echo $temp."\n";

    $temp = ($b) ? yield 4 : 8;
    echo $temp."\n";

    $temp = ($b) ? 16 : yield 32;
    echo $temp."\n";

    $temp = ($a) ? yield 64 : 128;
    echo $temp."\n";
}

$gen = f(true, true);
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send(10)."\n";
echo "s:".$gen->send(20)."\n";

echo "s:".$gen->send(30)."\n";
echo "s:".$gen->send(40)."\n";

echo "Done.";






