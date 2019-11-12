<?php
namespace variables\isset_002;

class LoggedArrayAccess implements \ArrayAccess
{
    private $arr;

    public function __construct() {
        $this->arr = [];
    }

    public function offsetExists ($offset) {
        echo __FUNCTION__, "\n";
        return isset($this->arr[$offset]);
    }
    public function offsetGet ($offset) {
        echo __FUNCTION__, "\n";
        return $this->arr[$offset];
    }
    public function offsetSet ($offset , $value) {
        echo __FUNCTION__, "\n";
        $this->arr[$offset] = $value;
    }
    public function offsetUnset ($offset) {
        echo __FUNCTION__, "\n";
        unset($this->arr[$offset]);
    }
}

function test($a, $offset) {
    $a[$offset] = 'bar';
    unset($a[$offset]);
    echo isset($a[$offset]);
}

test(new LoggedArrayAccess(), "foo");
test(new \SplObjectStorage(), new \stdClass());

echo "Done.";
