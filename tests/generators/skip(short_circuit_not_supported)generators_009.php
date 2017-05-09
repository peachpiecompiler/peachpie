<?php

function f()
{
    if((yield 1) == 10 || (yield 2) == 20){
        echo "Y\n";
    }
    
    if((yield 1) == 10 && (yield 2) == 20){
        echo "N\n";
    }
    else {
        echo "Y\n";
    }
}

$gen = f();
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send(10)."\n";
echo "s:".$gen->send(15)."\n";

