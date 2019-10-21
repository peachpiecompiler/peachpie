<?php
namespace functions\anonymous_003;

$message = 'hello';

// Inherit $message
$example = function () use ($message) {
    print_r($message);
};
$example();

// Inherited variable's value is from when the function
// is defined, not when called
$message = 'world';
$example();

// Reset message
$message = 'hello';

// Inherit by-reference
$example = function () use (&$message) {
    print_r($message);
};
$example();

// The changed value in the parent scope
// is reflected inside the function call
$message = 'world';
$example();

// Closures can also accept regular arguments
$example = function ($arg) use ($message) {
    print_r($arg . ' ' . $message);
};
$example("hello");
