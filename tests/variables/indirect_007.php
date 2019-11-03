<?php
namespace variables\indirect_007;

$a = "b";
$$a = "Stored via indirect variable.";
echo $$a." ".$b;
