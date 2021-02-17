<?php
namespace variables\valueref_sscanf;

$returnVal = sscanf('user/login', '%[^/]/%s', $c, $m);

echo $c, PHP_EOL;
echo $m, PHP_EOL;

echo "Done.";
