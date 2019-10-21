<?php
namespace operators\ternary_ex;

$a = array(

function_exists('key') ? 1:0 => function_exists('key') ? 1:0,
function_exists('unknown') ? 1:0 => function_exists('unknown') ? 1:0

);

$b = array(1,2,3,4);

$c = array(1=>1,2=>2);

$d = $a ? 1 : 2;

print_r($a);
print_r($b);
print_r($c);
print_r($d);
