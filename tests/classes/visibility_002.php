<?php
namespace classes\visibility_002;

$errname = \Error::class;
echo method_exists(\Error::class, "GetType") ? "fail" : "ok.", PHP_EOL; // evaluated in compile time
echo method_exists($errname, "GetType") ? "fail" : "ok.", PHP_EOL; // check in runtime

echo "Done.";
