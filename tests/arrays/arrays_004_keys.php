<?php
namespace arrays\arrays_004;

function testarr(){
	$a = ['+1' => 'test']; // https://github.com/peachpiecompiler/peachpie/issues/1084
    echo
        isset($a[1]) ? 'fail' : 'ok',
        PHP_EOL,
        isset($a['+1']) ? 'ok' : 'fail'
        ;
}

testarr();

echo PHP_EOL, "Done.";
