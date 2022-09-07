<?php
namespace operators\assign_005;

class X
{
    function a()
    {
        $this[1] = 2;   // https://github.com/peachpiecompiler/peachpie/issues/1015
        echo $this[1];
    }
}

class Y extends X implements \ArrayAccess
{
    
	function offsetExists($offset) {
	}
	
	function offsetGet($offset) {
        echo "get[$offset]", PHP_EOL;
	}
	
	function offsetSet($offset, $value) {
        echo "set[$offset] = $value", PHP_EOL;
	}
	
	function offsetUnset($offset) {
	}
}

(new Y)->a();

