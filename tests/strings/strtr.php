<?php
namespace strings\strtr;

$addr = "this is some address.\n";
echo strtr($addr, "aaimm", "AEIOU");

$trans = array("hello" => "hi", "hi" => "hello");
echo strtr("hi all, I said hello\n", $trans);

// hello all, I said hi

$trans = array_flip(array("a" => "e", "b" => "e"));
echo strtr("hi all, I said hello\n", $trans);

$trans = array("a" => 1, "e" => false, 1 => "ONE", 3.14 => round(M_PI,5));
echo strtr("12[3]45, hi all, I said hello\n", $trans);
