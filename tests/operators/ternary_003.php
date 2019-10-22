<?php
namespace operators\ternary_003;

function dump($x) {
    print_r($x);
}

function test($a) {
    dump($a ? "foo" : null);
}

test(false);
