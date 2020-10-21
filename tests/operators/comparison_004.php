<?php
namespace operators\comparison_004;

function test()
{
    // https://github.com/peachpiecompiler/peachpie/issues/775
    echo chr(161) == chr(160) ? "ok" : "161 vs 160 fail", PHP_EOL;
}

test();

echo "Done.";
