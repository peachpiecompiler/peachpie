<?php
namespace pcre\preg_match_004;

echo preg_match("~^\S~", "\nA") ? 1 : 0;
echo preg_match("~^\s~", "\nA") ? 1 : 0;
echo preg_match("~^\n~", "\nA") ? 1 : 0;

echo "\n";

echo preg_match("~^.~s", "\n") ? 1 : 0;
echo preg_match("~^.~", "\n") ? 1 : 0;

echo "\n";

echo "Done";
