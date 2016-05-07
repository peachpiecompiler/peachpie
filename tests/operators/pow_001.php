<?php

function test($a, $b, $c, $d)
{
	echo $a ** $b, ',', (int)($c ** $d), ',', $a ** $c, ',', $c ** $b;
	echo $a ** 1.2;
	echo $c ** 2;
	echo (int)(2 ** $d);
}

test(1, 2, 3.4, 5.6);

echo "Done";
