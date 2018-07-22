<?php

function fib(int $no) {
    if ($no == 0 || $no == 1) {
        return $no;
    } else {
        return fib($no - 2) + fib($no - 1);
    }
}

/*|double|integer|*/$res = fib(40);
