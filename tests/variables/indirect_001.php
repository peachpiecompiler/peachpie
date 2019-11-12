<?php
namespace variables\indirect_001;

$x = "a";
$$x = 56;
echo $$x." ".$a;
