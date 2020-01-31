<?php
// Tests DES standart and extanded of crypt function
//Standart
echo crypt('rasmuslerdorf', 'rl') . "\n";
echo crypt('differentPassword', 'aa') . "\n";
//Extended
echo crypt('differentPassword', '_J...sasd') . "\n";
echo crypt('rasmuslerdorf', '_J9..rasm') . "\n";