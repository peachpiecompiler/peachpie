<?php
namespace strings\number_format;

echo number_format(1234.567), PHP_EOL;
echo number_format(1234.567, -4), PHP_EOL;
echo number_format(1234.567, 4), PHP_EOL;
echo number_format(1234.567, 2, " with decimals ", " ' "), PHP_EOL;

echo "Done.";
