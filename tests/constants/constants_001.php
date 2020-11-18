<?php
namespace constants\constants_001;

define("X", 123);
echo constant("X"), PHP_EOL;
echo X, PHP_EOL;
echo defined("X") ? "1" : "0", PHP_EOL;
echo defined("\\X") ? "1" : "0", PHP_EOL;

echo "Done.";
