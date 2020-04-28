<?php
namespace hash\password_needs_rehash_002;

$password = "rasmuslerdorf";
$memory_cost = 512;
$time_cost = 11;
$threads = 1;

$options = [
    'threads' => $threads,
    'time_cost' => $time_cost,
    'memory_cost' => $memory_cost,
];

$hash = password_hash( $password, PASSWORD_ARGON2ID, $options);

if (password_verify($password, $hash)) {
    // Check if a newer hashing algorithm is available
    // or the cost has changed
    $options['time_cost'] =  11.3;
    if (password_needs_rehash($hash, PASSWORD_ARGON2ID, $options)) {
        echo 'Rehash';
    }
    else {
        echo 'Ok';
    }
}