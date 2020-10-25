<?php
namespace strings\double_001; 

echo INF, PHP_EOL; // INF
echo -INF, PHP_EOL; // -INF
echo NAN, PHP_EOL; // NAN

$s = (string)INF;
echo $s, PHP_EOL; // INF

echo "Done.";
