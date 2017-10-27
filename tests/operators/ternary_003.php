<?php

function dump($x) {
    var_dump($x);
}

function test($a) {
    dump($a ? "foo" : null);
}

test(false);
