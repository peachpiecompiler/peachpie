<?php
namespace generators\generators_014;

function from() {
    yield 1; // key 0
    yield 2; // key 1
    yield 3; // key 2
}
function gen() {
    yield 0; // key 0
    yield from from(); // keys 0-2
    yield 4; // key 1
    yield 5 => 5;
    yield 4 => 4;
    yield 6;
}
print_r(iterator_to_array(gen()));
