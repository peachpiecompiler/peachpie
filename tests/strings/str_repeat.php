<?php
namespace strings\str_repeat;

$a = "\x88\x99";
$b = str_repeat($a, 8);

echo $b[3] == "\x99" ? "ok" : "fail";

echo "Done.";