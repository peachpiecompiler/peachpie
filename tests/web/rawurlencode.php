<?php
namespace web\rawurlencode;

print_r(rawurlencode('/+=._ ;'));
echo PHP_EOL;

print_r(rawurlencode('foo @+%/'));
echo PHP_EOL;

echo 'Done.';
