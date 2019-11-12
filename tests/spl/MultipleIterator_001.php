<?php
namespace spl\MultipleIterator_001;

function test() {
    $it1 = new \ArrayIterator(array("foo" => "123", "bar" => "456", "baz" => "789"));
    $it2 = new \ArrayIterator(array("bar" => "456", "baz" => "789"));

    $mit = new \MultipleIterator(\MultipleIterator::MIT_NEED_ANY | \MultipleIterator::MIT_KEYS_ASSOC);
    $mit->attachIterator($it1, "it1");
    $mit->attachIterator($it2, "it2");

    foreach ($mit as $key => $val) {
        print_r($key);
        print_r($val);
    }

    $mit = new \MultipleIterator(\MultipleIterator::MIT_NEED_ALL | \MultipleIterator::MIT_KEYS_NUMERIC);
    $mit->attachIterator($it1, "it1");
    $mit->attachIterator($it2);

    foreach ($mit as $key => $val) {
        print_r($key);
        print_r($val);
    }
}

test();
