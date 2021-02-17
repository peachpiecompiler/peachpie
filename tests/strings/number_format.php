<?php
namespace strings\number_format;

echo number_format(1234.567), PHP_EOL;
echo number_format(1234.567, -4), PHP_EOL;
echo number_format(1234.567, 4), PHP_EOL;
echo number_format(1234.567, 2, " with decimals ", " ' "), PHP_EOL;

$null = null;
echo number_format($null), PHP_EOL; // allows NULL as argument // https://github.com/peachpiecompiler/peachpie/issues/839

echo "Done.";
