<?php
namespace generators\generators_002;
class A {

    public $a = 5;
    public function getVal(){
        return 2;
    }


    public function foo() {
        $this->a = 10;
        yield 1 + $this->getVal();
    }



    public static $b = 5;
    public static function sgetVal(){
        return 4;
    }
    public static function sfoo(){
        self::$b = 10;
        yield 2 + self::sgetVal();
    }


    public function run_foo(){
        echo "a:".$this->a."\n";

        $gen = $this->foo();
        echo "k:".$gen->key()."v:".$gen->current()."\n";

        foreach($this->foo() as $value){
            echo "v:".$value."\n";
        }

        echo "a:".$this->a."\n";
    }

    public static function run_sfoo(){
        echo "b:".self::$b."\n";

        $gen = A::sfoo();
        echo "k:".$gen->key()."v:".$gen->current()."\n";

        foreach(A::sfoo() as $value){
            echo "v:".$value."\n";
        }

        echo "b:".self::$b."\n";

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

echo "Done.";

