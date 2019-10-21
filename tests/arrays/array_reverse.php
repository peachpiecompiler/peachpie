<?php
namespace arrays\array_reverse;

print_r( array_reverse([1,2,3]) );
print_r( array_reverse([1,2,3], true) );
print_r( array_reverse([1,2,"three" => 3], true) );
