<?php
namespace reflection\class_parent_001;

class foo {}
class bar extends foo {}
class baz extends bar {}

print_r(class_parents(new baz));

class dad {

}

class child extends dad {
    function __construct()
    {
        echo "I'm " , get_parent_class($this) , "'s son\n";
    }
}

class child2 extends dad {
    function __construct()
    {
        echo "I'm " , get_parent_class('child2') , "'s son too\n";
    }
}

new child;
new child2;
