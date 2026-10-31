<?php

// https://github.com/peachpiecompiler/peachpie/issues/1148

namespace arrays\array_key_last_001;

// Sample associative array
$fruits = [
    'apple' => 'green',
    'banana' => 'yellow',
    'cherry' => 'red',
];

// Using array_key_last() to get the last key of the array
$lastKey = array_key_last($fruits);

echo "The last key is: " . $lastKey . "\n";
echo "The value of the last element is: " . $fruits[$lastKey] . "\n";

// Sample indexed array
$numbers = [10, 20, 30, 40];

// Using array_key_last() to get the last key of the indexed array
$lastIndex = array_key_last($numbers);

echo "The last index is: " . $lastIndex . "\n";
echo "The value of the last element is: " . $numbers[$lastIndex] . "\n";
