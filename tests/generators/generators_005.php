<?php
namespace generators\generators_005;

function f()
{
 	$a = yield 1;
    echo "s1:".$a."\n";

    $b = yield 2;
    echo "s2:".$b."\n";

    $c = yield 3; 
    echo "s3:".$c."\n"; 

    yield "4";
    echo "s4:<>\n";

    return $a;
}

$gen = f();
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send("A")."\n";
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "n:".$gen->next()."\n";
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send(3)."\n";
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send(4)."\n";
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "r:".$gen->getReturn()."\n";

echo "Done.";

