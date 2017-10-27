<?php

function dump($x) {
    print_r($x);
}

function test($a) {
    dump($a ? "foo" : null);
}

test(false);
