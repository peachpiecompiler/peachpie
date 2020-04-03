<?php
namespace functions\__call_001;

class X
{
    public static function __callStatic($name, $args)
    {
        echo __METHOD__, "\n";
        print_r($name);
		print_r($args);
        return $name;
    }

    public function __call($name, $args)
    {
        echo __METHOD__, "\n";
        print_r($name);
		print_r($args);
        return $name;
    }

    public function bar2($arg)
    {
		echo __METHOD__, "\n";
        echo $arg;
    }

    public function bar()
    {
		echo __METHOD__, "\n";
        return $this->bar3("hello",1,2,3,4,5,6,7,8,9) . $this->bar2("xxx");
    }
}

if (!isset($ggg)) {
    // incomplete class decl
    class Y extends X
    {
        public static function fooex($a)
        {
			echo __METHOD__, "\n";
            echo "fooex($a)/", parent::bar3(5,5,5), X::bar4(6,6,6);   // should not be called statically
        }

        public function yoo($a)
        {
			echo __METHOD__, "\n";
            return parent::bar3(1,1,1);// call it through __call, not __callStatic
        }
    }
}

class Z extends Y
{
    public function zoo2()
    {
		echo __METHOD__, "\n";
        return "zoo2/" . $this->nonexistingzoo(8,8,8);
    }

    public function zoo()
    {
		echo __METHOD__, "\n";
        return $this->bar() . $this->zoo2() . X::nonexisting1(3);
    }
}

// calling static methods resolved in compile time
echo X::nonexistingfoo(1,2,3);
echo call_user_func_array ( array(__NAMESPACE__ ."\\X","nonexisting2"), array(10,20,30) );

echo Y::nonexistingfoo(1,2,3);
echo Y::nonexistingfoo2(4);
echo call_user_func_array ( array(__NAMESPACE__ ."\\Y","nonexisting3"), array("a",'b','c') );

echo Z::nonexistingstatic();

// calling instance methods
$x = new X();
echo $x->bar();
echo $x->non_existing_func(1,2,3);
echo call_user_func( array($x,"foo"), 5,6,7 );

$y = new Y();
echo $y->bar();
echo $y->nonexisting4(9,8,7);
echo call_user_func( array($y,"bar"), 4,5,6);
echo call_user_func( array($y,"nonexisting5"), 333,666,999 );

$z = new Z();
echo $z->zoo();
