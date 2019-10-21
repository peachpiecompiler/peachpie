<?php
namespace operators\cast_bool;

function test($x){ echo $x ? "1" : "0"; }

test((bool) "");        // bool(false)
test((bool) 1);         // bool(true)
test((bool) -2);        // bool(true)
test((bool) "foo");     // bool(true)
test((bool) 2.3e5);     // bool(true)
test((bool) array(12)); // bool(true)
test((bool) array());   // bool(false)
test((bool) "false");   // bool(true)
