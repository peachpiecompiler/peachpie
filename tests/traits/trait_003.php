<?php
namespace traits\trait_003;

// Example #5 Conflict Resolution

trait A {
    public function smallTalk() {
        echo 'a';
    }
    public function bigTalk() {
        echo 'A';
    }
}

trait B {
    public function smallTalk() {
        echo 'b';
    }
    public function bigTalk() {
        echo 'B';
    }
}

class Talker {
    use A, B {
        B::smallTalk insteadof A;
        A::bigTalk insteadof B;
        B::bigTalk as talk;
    }
}

function test(Talker $x)
{
    echo
    get_class($x),
    ":", 
    $x->smallTalk(),
    $x->bigTalk(),
    $x->talk(),
    "\nDone.";
}

test(new Talker);
