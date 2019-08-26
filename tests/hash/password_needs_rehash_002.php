<?php
$password = "rasmuslerdorf";
$memory_cost = 512;
$time_cost = 11;
$threads = 3;

$options = [
    'threads' => $threads,
    'time_cost' => $time_cost,
    'memory_cost' => $memory_cost,
];

$hash = password_hash( $password, PASSWORD_ARGON2ID, $options);

if (password_verify($password, $hash)) {
    // Check if a newer hashing algorithm is available
    // or the cost has changed
    $options['time_cost'] =  10;
    if (password_needs_rehash($hash, PASSWORD_ARGON2ID, $options)) {
        // If so, create a new hash, and replace the old one
        echo 'Success';
    }
    else
    {
        echo 'Failed';
    }
    // Log user in
}

?>