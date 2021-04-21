<?php
namespace strings\mb_substr;

$null = NULL;

echo mb_substr("hello", 1), "\n";
echo mb_substr("hello", 1, NULL), "\n";
echo mb_substr("hello", 1, -1), "\n";
echo mb_substr("hello", 1, $null), "\n";
echo mb_substr("\x88\x99\xccHello", 3, null, "8bit"), "\n";

echo "Done.";
