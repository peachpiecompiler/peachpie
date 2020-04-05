<?php
namespace arrays\max_key;

$a = [PHP_INT_MAX => 'foo'];
@$a[] = 'bar'; // won't be added
print_r(count($a)); // 1
