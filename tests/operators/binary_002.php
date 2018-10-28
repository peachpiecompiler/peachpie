<?php

class C {
    const C1 = 1 | 1 << 1;
    const C2 = 1 & 1 << 1;
    const C3 = 1 ^ 1 << 1;
}

function test() {
    echo C::C1;
    echo C::C2;
    echo C::C3;
}

test();