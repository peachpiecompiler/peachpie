<?php
namespace spl\RegexIterator_001;

class A {
    public function __toString() {
        return "8546";
    }
}

function test() {
    $it = new \ArrayIterator(array("foo" => new A(), array("bla" => 856), "bar", "832", 8123, 8.12));
    $it = new \RegexIterator($it, "/^8/");
    print_r(iterator_to_array($it));

    // Examples from https://adayinthelifeof.nl/2014/02/12/spl-deepdive-regexiterator/

    $it = new \ArrayIterator(array("foo" => "123", "bar" => "456", "baz" => "789"));
    $it = new \RegexIterator($it, "/^ba/");
    print_r(iterator_to_array($it));

    $it = new \ArrayIterator(array("foo" => "123", "bar" => "456", "baz" => "789"));
    $it = new \RegexIterator($it, "/^ba/", \RegexIterator::MATCH, \RegexIterator::USE_KEY);
    print_r(iterator_to_array($it));

    $it = new \ArrayIterator(array("foo" => "123", "bar" => "456", "baz" => "789"));
    $it = new \RegexIterator($it, "/^ba/", \RegexIterator::MATCH, \RegexIterator::INVERT_MATCH);
    print_r(iterator_to_array($it));

    $it = new \ArrayIterator(array("foo" => "123", "bar" => "456", "baz" => "789"));
    $it = new \RegexIterator($it, "/^ba/", \RegexIterator::MATCH, \RegexIterator::USE_KEY | \RegexIterator::INVERT_MATCH);
    print_r(iterator_to_array($it));

    $it = new \ArrayIterator(array("foo", "bar", "bazbar"));
    $it = new \RegexIterator($it, "/^ba(.)/", \RegexIterator::GET_MATCH);
    print_r(iterator_to_array($it));

    $it = new \ArrayIterator(array("tmp", "foo", "bar", "bazbar"));
    $it = new \RegexIterator($it, "/ba(.)/", \RegexIterator::ALL_MATCHES);
    print_r(iterator_to_array($it));

    $it = new \ArrayIterator(array("tmp", "foo", "bar", "bazbar"));
    $it = new \RegexIterator($it, "/a/", \RegexIterator::SPLIT);
    print_r(iterator_to_array($it));

    $it = new \ArrayIterator(array("tmp", "foo", "bar", "bazbar"));
    $it = new \RegexIterator($it, "/a/", \RegexIterator::REPLACE);
    print_r(iterator_to_array($it));

    $a = new \ArrayIterator(array('test1', 'test2', 'test3'));
    $i = new \RegexIterator($a, '/^(test)(\d+)/', \RegexIterator::REPLACE);
    $i->replacement = '$2:$1';
    print_r(iterator_to_array($i));
}

test();
