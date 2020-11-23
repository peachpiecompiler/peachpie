<?php
namespace constants\builtin_002;

// PHP_SAPI: lazily defined constant
echo defined("PHP_SAPI") ? 1 : 0, PHP_EOL; // 1
echo gettype(PHP_SAPI), PHP_EOL; // string
echo gettype(constant("PHP_SAPI")), PHP_EOL; // string

//
echo "Done.";
