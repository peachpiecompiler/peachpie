<?php
namespace operators\cast_002;

interface I
{
}

function foo(I $i = null) { print_r($i); }

function test($a) {
  foo($a);
}

test(null);

echo "Done.";
