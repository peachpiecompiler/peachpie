<?php
namespace operators\sub_001;

function f($iSbdSize)
{
    if ($iSbdSize > 0)
    {
        for ($i = 0; $i < ($iSbdSize - 1); $i++) // https://github.com/peachpiecompiler/peachpie/issues/993
        {
            echo $i, '.';
        }
    }
}

f(2.0);

echo 'Done.';