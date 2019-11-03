<?php
namespace spl\RecursiveRegexIterator_001; 

class A {
    public $bar = 88;
    public $boo = "bla";

    public function __toString() {
        return "8546";
    }
}

function test() {
    $it = new \RecursiveArrayIterator(array("foo" => new A(), array("bla" => 856, "bal" => "no"), "bar", "832", 8123, 8.12));
    $it = new \RecursiveRegexIterator($it, "/^8/");
    print_r(iterator_to_array($it));

    $it = new \RecursiveIteratorIterator($it);
    print_r(iterator_to_array($it));
}

test();
