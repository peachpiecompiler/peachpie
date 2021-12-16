<?php
namespace arrays\array_shift_001;

$a[] = 0;
array_shift($a);
$a[] = 1;
print_r( $a ); // expected array( 0 => 1 )
