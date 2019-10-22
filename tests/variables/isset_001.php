<?php
namespace variables\isset_001;

$a = new \stdClass;
$a->p = new \stdClass;
$a->p->p = 123;

echo isset($a) ? 1 : 0;
echo isset($a->p) ? 1 : 0;
echo isset($a->p->p) ? 1 : 0;

echo "Done.";
