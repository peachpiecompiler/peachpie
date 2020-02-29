<?php
namespace functions\static_call_003;

class X {
    function equals($a) {
        return true;
    }
}

class Y extends X {
    function test(Y $target) {
        if (parent::equals($target)) echo "Ok.";
    }
}

(new Y)->test(new Y);

echo "Done.";
