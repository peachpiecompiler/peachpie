<?php
namespace spl\CachingIterator_001;

function display_state(\CachingIterator $it) {
    echo "Current: ". $it->key() ." => ". $it->current() ."  ". ($it->valid() ? "VALID" : "INVALID") ."\n";
	echo "Inner: ". $it->getInnerIterator()->key() ." => ". $it->getInnerIterator()->current() ."  ". ($it->getInnerIterator()->valid() ? "VALID" : "INVALID") ."\n";
	echo "HasNext: ". (int)$it->hasNext() ."\n";
	echo "Count: ". $it->count() ."\n";
    echo "Cache:\n";
    print_r($it->getCache());
    echo "\n";
}

function test() {
	$iterator = new \ArrayIterator(array(1, "bla" => 5, 2, null));
	$cache = new \CachingIterator($iterator, \CachingIterator::FULL_CACHE);

    display_state($cache);
    foreach ($cache as $key => $val) {
        echo "Vars: ". $key ." => ". $val ."\n";
        display_state($cache);
    }
    display_state($cache);
    $cache->offsetUnset($key);
    print_r($cache->getCache());
}

test();
