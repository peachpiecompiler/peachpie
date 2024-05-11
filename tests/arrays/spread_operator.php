<?php
namespace arrays\spread_operator;

function test() {

    $nums = [1,2,3];
    $keyed = ['a' => 1, 'b' => 2, 'c' => 3];
    $traversable = new \ArrayObject([1,2,3]);

    print_r( [1, 2, 3, ...$nums] );
    print_r( [1, 2, 3, ...$keyed] );
    print_r( [1, 2, 3, ...$traversable] );
    print_r( [...$nums, 0, 0, 0] );
}

test();
echo 'Done.';