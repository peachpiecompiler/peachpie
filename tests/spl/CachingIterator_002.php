<?php
namespace spl\CachingIterator_002;

class MyArrayIterator extends \ArrayIterator
{
    function __toString() {
        return $this->key() . ':' . $this->current();
    }
}

class A
{
    function __toString() {
        echo "A::__toString() called\n";
        return "This is A";
    }    
}

function test($flags) {
    $iterator = new MyArrayIterator(array(1, "bla" => 5, 2, null, new A()));
    $cache = new \CachingIterator($iterator, $flags);

    foreach ($cache as $item) {
        echo " ";                   // To determine when is A::__toString() called
        echo $cache;
        echo "\n";
    }

    echo "\n";
}

test(\CachingIterator::CALL_TOSTRING);
test(\CachingIterator::TOSTRING_USE_KEY);
test(\CachingIterator::TOSTRING_USE_CURRENT);
test(\CachingIterator::TOSTRING_USE_INNER);
