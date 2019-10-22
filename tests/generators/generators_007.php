<?php
namespace generators\generators_007;

function bar($txt){
    echo $txt;
    return 1;
}

function f()
{
    echo "PRE\n";
    $var = bar("pre\n")+(yield 1)+bar("pos\n"); 
    echo "POS\n";
    echo "sv:".$var."\n";

    switch(bar("pre2\n")+(yield 1)+bar("pos2\n"))
    {
        case 1:
            echo "5N\n";
            break;
        case bar("pre3\n")+(yield 1)+bar("pos3\n"):
            echo "3Y\n";
            break;
        case 3:
            echo "7N\n";
            break;
        default:
            echo "8N\n";
            break;
    } 
}

$gen = f();
echo "k:".$gen->key()."v:".$gen->current()."\n";
echo "s:".$gen->send(1)."\n";
echo "s:".$gen->send(2)."\n";
echo "s:".$gen->send(2)."\n";

echo "--------------------------\n";

function g()
{
    $altVar = 5;
    $val = (++$altVar) + (yield $altVar) + ($altVar++);

    echo $val."\n";
}

$gen = g();
echo "k:".$gen->key()."v:".$gen->current()."\n";
echo "s:".$gen->send(5)."\n";

echo "--------------------------\n";

function h()
{

    switch(bar("pre2\n")+bar(yield 1)+bar("pos2\n"))
    {
        case 1:
            echo "5N\n";
            break;
        case bar("pre3\n")+(yield 1)+bar("pos3\n"):
            echo "3N\n";
            break;
        case 3:
            echo "7Y\n";
            break;
        default:
            echo "8N\n";
            break;
    } 
}

$gen = h();
echo "k:".$gen->key()."v:".$gen->current()."\n";
echo "s:".$gen->send("AAAAA\n")."\n";
echo "s:".$gen->send(100)."\n";

echo "--------------------------\n";

function i()
{
    switch(bar("Just once\n"))
    {
        case yield 1:
            echo "5N\n";
            break;
        case yield 2:
            echo "3Y\n";
            break;
        default:
            echo "8N\n";
            break;
    } 
}

$gen = i();
echo "k:".$gen->key()."v:".$gen->current()."\n";
echo "s:".$gen->send(0)."\n";
echo "s:".$gen->send(1)."\n";

echo "Done.";




