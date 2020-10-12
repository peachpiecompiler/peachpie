<?php

namespace classes\properties_002;

class X
{
    var $f = A ? 1 : 2; // conditional expression in field initializer (no routine context)
}

define("A", false);

echo (new X)->f; // 2

echo "Done.";
