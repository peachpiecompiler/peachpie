<?php
namespace operators\comparison_004;

function test()
{
    // https://github.com/peachpiecompiler/peachpie/issues/775
    echo chr(161) == chr(160) ? "fail" : "ok", PHP_EOL;
}

test();

echo "Done.";
