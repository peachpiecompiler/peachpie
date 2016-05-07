<?php

function test($a, $b, $c, $d)
{
	echo $a ** $b, ',', (int)($c ** $d), ',', $a ** $c, ',', $c ** $b;
}

test(1, 2, 3.4, 5.6);

echo "Done";
