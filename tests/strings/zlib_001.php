<?php
namespace strings\zlib_001;

$original = str_repeat("hallo php", 48);
$packed = gzencode($original);

echo strlen($packed)." ".strlen($original). "\n";

$decoded = gzdecode($packed);
if (strcmp($original, $decoded) == 0) echo "Strings are equal";
