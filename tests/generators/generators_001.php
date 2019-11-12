<?php
namespace generators\generators_001;

function f()
{
    echo "preY\n";
	yield "a";
    for ($i = 1; $i <= 3; $i++)
    {
        yield 5 => 2;
    }
    yield "7" => "c";
    yield;
    if ($i > 2) { yield "d"; }
    echo "posY\n";
}

$gen = f();
foreach($gen as $key => $value){
    echo "k:".$key."v:".$value."\n";
}

echo "r:".$gen->getReturn()."\n";
echo "k:".$gen->key()."v:".$gen->current()."c:".$gen->valid()."\n";

echo "-------------------------------------------------\n";

function h()
{
    echo "preY\n";
	yield "a";
    yield;
    yield "b";
    echo "posY\n";

    return 2;
}

$gen = h();
echo "1\n";

echo "k:".$gen->key()."v:".$gen->current()."c:".$gen->valid()."\n";
echo "2\n";

$gen->next();
echo "k:".$gen->key()."v:".$gen->current()."c:".$gen->valid()."\n";
echo "3\n";

$gen->next();
echo "k:".$gen->key()."v:".$gen->current()."c:".$gen->valid()."\n";
echo "4\n";

$gen->next();
echo "k:".$gen->key()."v:".$gen->current()."c:".$gen->valid()."\n";
echo "r:".$gen->getReturn()."\n";
echo "5\n";

$gen->next();
echo "k:".$gen->key()."v:".$gen->current()."c:".$gen->valid()."\n";
echo "r:".$gen->getReturn()."\n";
echo "6\n";

echo "-------------------------------------------------\n";

function g()
{
	yield 1;
    return "a";
}

$gen = g();
foreach($gen as $key => $value){
    echo "k:".$key."v:".$value."\n";
}

echo "r:".$gen->getReturn()."\n";
echo "k:".$gen->key()."v:".$gen->current()."c:".$gen->valid()."\n";

echo "Done.";

