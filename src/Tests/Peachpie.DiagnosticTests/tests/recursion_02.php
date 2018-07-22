<?php

function pad_left(string $a) {
    if (strlen($a) < 5) {
        return $a;
    } else {
        return pad_right(substr($a, 1));
    }
}

function pad_right(string $a) {
    if (strlen($a) < 5) {
        return $a;
    } else {
        return pad_left(substr($a, 0, strlen($a) - 1));
    }
}

/*|string|*/$res = pad_right("Lorem Ipsum");
