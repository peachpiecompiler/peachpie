<?php
namespace generators\generators_006;

function f()
{
    if ((yield 1) == "A"){
        echo "1Y\n";
    }

    if ((yield 2) == "B"){
        echo "2N\n";
    }
    else if ((yield 3) == "C"){
        echo "2Y\n";
    }
    else if ((yield 4) == "D"){
        echo "12N\n";
    }
    else {
        echo "4N\n";
    }

    $tmp = 2;
    switch($tmp)
    {
        case 1:
            echo "5N\n";
            break;
        case yield 5:
            echo "3Y\n";
            break;
        case 3:
            echo "7N\n";
            break;
        default:
            echo "8N\n";
            break;
    }  
    
    $tmp = 20;
    switch($tmp)
    {
        case yield 6:
            echo "4Y\n";
        case yield 7:
            echo "5Y\n";
            break;
        default:
            echo "13N\n";
            break;
    } 

    switch(yield 4)
    {
        case 100:
            echo "6Y\n";
            break;
        case 200:
            echo "10N\n";
            break;
        default:
            echo "11N\n";
            break;
    }   

    foreach(yield 8 as $num){
        echo "xY:".$num."\n";
    }

}

$gen = f();
echo "k:".$gen->key()."v:".$gen->current()."\n";

echo "s:".$gen->send("A")."\n";

echo "s:".$gen->send("C")."\n";
echo "s:".$gen->send("C")."\n";

echo "s:".$gen->send(2)."\n";

echo "s:".$gen->send(20)."\n";

echo "s:".$gen->send(100)."\n";

echo "s:".$gen->send([2, 3, 4])."\n";

echo "Done.";


