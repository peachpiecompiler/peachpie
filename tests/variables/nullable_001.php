<?php
namespace variables\nullable_001;

function test(): ?bool { // CLR Nullable<bool>
    return true;
}

function test2(): ?float { // CLR Nullable<double>
    return 123.456;
}

function test3(): ?double { // CLR Nullable<double>
    return null;
}

$value = test(); // converts bool? -> bool|null

echo $value, PHP_EOL;
echo test(), PHP_EOL;
echo (string)test(), PHP_EOL;
echo round(test2()), PHP_EOL;
echo "'", test3(), "'", PHP_EOL;

echo "Done.";
