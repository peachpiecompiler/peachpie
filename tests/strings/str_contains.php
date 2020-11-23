<?php
namespace strings\str_contains;

function test( string $haystack , string $needle, bool $expected ) {
    if( function_exists("str_contains") ) {
        echo str_contains($haystack, $needle) ? 1 : 0;
    }
    else {
        echo $expected ? 1 : 0;
    }
    echo PHP_EOL;
}

test('abc', 'a', true);
test('abc', 'd', false);
test('abc', 'bc', true);
test('abc', 'abcdef', false);

// $needle is an empty string
test('abc', '', true);
test('', '', true);

//
echo "Done.";