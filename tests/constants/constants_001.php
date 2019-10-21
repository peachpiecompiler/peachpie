<?php
namespace constants\constants_001;

define("X", 123);
echo constant("X");
echo X;
echo defined("X") ? "1" : "0";

echo "Done.";
