<?php
namespace arrays\array_merge;

print_r( array_merge() ); // empty array
print_r( array_merge([1], [2]) ); // [1, 2]
print_r( array_merge(["a" => 1], ["a" => 2]) ); // ["a" => 2]
