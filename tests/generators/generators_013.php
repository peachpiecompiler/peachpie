<?php
namespace generators\generators_013;

function f()
{
    $var = "<yieldRewriter>0";
    $$var = 10;
    1 + yield;

    echo "indirectValue:".$$var."\n";
    foreach(get_defined_vars() as $key => $value){
        echo "localVariable:".$key.":".$value."\n";
    }
}

$gen = f();
echo "k:".$gen->key()."v:".$gen->current()."\n";
f()->next();

echo "Done.";
