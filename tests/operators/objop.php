<?php
namespace operators\objop;

class Foo{

}

class Bar{
	var $x = 1;
}

error_reporting(0);

$x = new Foo();
$y = new Bar();

print_r(!$x);
print_r(!$y);

print_r(-$x);
print_r(-$y);

print_r(+$x);
print_r(+$y);

// not supported yet:
// print_r($x++);
// print_r($y++);
// print_r(++$x);
// print_r(++$y);


print_r( $x + 1);
print_r( $y + 1);

print_r( $x + 1.1);
print_r( $y + 1.1);

print_r( $x + $y);

print_r( $x - 1);
print_r( $y - 1);

print_r( round($x - 1.1, 2));
print_r( round($y - 1.1, 2));

print_r( $x - $y);

print_r( $x * 1);
print_r( $y * 1);

print_r( $x * 1.1);
print_r( $y * 1.1);

print_r( $x * $y);

print_r( $x / 1);
print_r( $y / 1);

// print_r( $x / 1.1);
// print_r( $y / 1.1);

print_r( $x / $y);

print_r( $x % 1);
print_r( $y % 1);

print_r( $x % 1.1);
print_r( $y % 1.1);

print_r( $x % $y);
