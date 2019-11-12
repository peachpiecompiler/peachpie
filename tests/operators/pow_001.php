<?php
namespace operators\pow_001;

function test($a, $b, $c, $d)
{
	echo $a ** $b, ',', (int)($c ** $d), ',', $a ** $c, ',';
    echo round($c ** $b, 2), ',';
	echo $a ** 1.2, ',';
	echo round($c ** 2, 2), ',';
	echo (int)(2 ** $d), ',';
}

test(1, 2, 3.4, 5.6);

echo "Done";
