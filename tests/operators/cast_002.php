<?php

interface I
{
}

function foo(I $i = null) { print_r($i); }

function test($a) {
  foo($a);
}

test(null);

echo "Done.";
