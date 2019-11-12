<?php
namespace arrays\array_sum;

function f() {

$i = 0;
while ($i++ < 1000) {
	$a[] = $i;
	$b[] = (string)$i;
}
$s1 = array_sum($a);
$s2 = array_sum($b);

print_r($s1);
print_r($s2);

$j = 0;
while ($j++ < 100000) {
	$c[] = $j;
	$d[] = (string) $j;
}
$s3 = array_sum($c);
$s4 = array_sum($d);

print_r($s3);
print_r($s4);

}

f();
