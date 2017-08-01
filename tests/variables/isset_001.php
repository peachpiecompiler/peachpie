<?php

$a = new stdclass;
$a->p = new stdclass;
$a->p->p = 123;

echo isset($a) ? 1 : 0;
echo isset($a->p) ? 1 : 0;
echo isset($a->p->p) ? 1 : 0;

echo "Done.";
