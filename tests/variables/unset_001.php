<?php
namespace variables\unset_001;
function foo() 
{
    unset($GLOBALS['bar']);
}

$bar = "something";
foo();
echo @$bar, "Done.";
