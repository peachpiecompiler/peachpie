<?php
namespace variables\unset_003;

class X implements \ArrayAccess {
    private $container = [1,2,3];

    public function offsetSet($offset, $value) {
        $this->container[$offset] = $value;
    }

    public function offsetExists($offset) {
        return isset($this->container[$offset]);
    }

    public function offsetUnset($offset) {
        unset($this->container[$offset]);
    }

    public function offsetGet($offset) {
        return $this->container[$offset];
    }
}


function f($v)
{
    unset($v[0]);
    print_r($v);
}

f([1,2,3]);
f(null);
f(123);
f(true);
f(new X);
//f("hello");

/** the same test for type analysed variables */
function q()
{
    $a = [1,2,3];
    $n = null;
    $i = 123;
    $d = 123.456;
    $b = true;
    $x = new X;

    unset($a[1], $n[1], $i[1], $d[1], $b[0], $x[1]);

    print_r($a);
    print_r($n);
    print_r($i);
    print_r($b);
    print_r($x);
}

q();

echo "Done.";
