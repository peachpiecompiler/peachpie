<?php

function pad_left(string $a) {
    if (strlen($a) >= 5) {
        return pad_right(substr($a, 1));
    } else {
        return $a;
    }
}

function pad_right(string $a) {
    if (strlen($a) >= 5) {
        return pad_left(substr($a, 0, strlen($a) - 1));
    } else {
        return $a;
    }
}

/*|string|*/$res = pad_right("Lorem Ipsum");
