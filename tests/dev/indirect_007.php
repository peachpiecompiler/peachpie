<?php

$a = "b";
$$a = "Stored via indirect variable.";
echo $$a." ".$b;
