<?php
namespace generators\generators_009;

function f()
{
    if((yield 1) == 10 || (yield 2) == 20){
        echo "Y1\n";
    }
    
    if((yield 3) == 20 && (yield 4) == 40){
        echo "N\n";
    }
    else {
        echo "Y2\n";
    }

    $temp = ((yield 5) == 50) ? (100 + (yield 6)) : (yield 7);
    echo $temp."\n";
}

$gen = f();
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send(10)."\n";
echo "s:".$gen->send(15)."\n";

echo "s:".$gen->send(20)."\n";
echo "s:".$gen->send(30)."\n";


function g()
{
    $ex_var = 10;
    echo ($ex_var ?? (yield 1) + 200)."\n";
    echo (15 ?? (yield 2) + 100)."\n";
    
    echo ($var ?? (yield 3) + 10)."\n";
    echo (null ?? (yield 4) + 20)."\n";

}

$gen = g();
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send(4)."\n";
echo "s:".$gen->send(8)."\n";

echo "Done.";






