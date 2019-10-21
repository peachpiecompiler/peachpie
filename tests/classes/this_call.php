<?php
namespace classes\this_call;

class X {
	
    public function __call($name, $args) {
        print_r($name);
		print_r($args);        

        return 123;
    }

    protected function bar2($arg) {
        echo ("bar2".$arg);
    }

    public function bar() {
        return
            $this->bar2("hello",1,2,3,4,5,6,7,8,9) .
            $this->bar2("hello") .
            $this->bar3("hello",1,2,3,4,5,6,7,8,9);
    }
}

$x = new X();
echo $x->bar();
