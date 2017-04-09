<?php
#Tests generator methods return type optimization static & global ones should return Generator instead of PhpValue

class A {

    public function foo() {
        yield 1;
    }

    public static function sfoo(){
        yield 2;
    }

    public function run_foo(){

        $gen = $this->foo();
        echo "k:".$gen->key()."v:".$gen->current()."\n";

        foreach($this->foo() as $value){
            echo "v:".$value."\n";
        }
    }

    public static function run_sfoo(){

        $gen = A::sfoo();
        echo "k:".$gen->key()."v:".$gen->current()."\n";

        foreach(A::sfoo() as $value){
            echo "v:".$value."\n";
        }
    }
}

(new A)->run_foo();
A::run_sfoo();

function bar()
{
    yield 3;
}


$gen = bar();
echo "k:".$gen->key()."v:".$gen->current()."\n";
foreach(bar() as $value){
    echo "v:".$value."\n";
}

