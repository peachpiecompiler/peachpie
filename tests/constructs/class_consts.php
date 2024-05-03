<?php
namespace constructs\class_consts;

class Test
{
    const C = 1 !== 2 ? 'a' : 'b';
}

echo Test::C;
echo 'Done.';
