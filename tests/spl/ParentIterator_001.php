<?php
namespace spl\ParentIterator_001;

function test() {
    $it = new \RecursiveArrayIterator(array(1, 2, array(3, 4, array(5, 6, 7), 8), 9, array(10, 11)));
    $pit = new \ParentIterator($it);
    print_r(iterator_to_array($pit));
}

test();
