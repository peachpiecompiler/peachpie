<?php
/**Test PASSWORD_ARGON2ID with options. */
$password = "rasmuslerdorf";
$memory_cost = 512;
$time_cost = 4;
$threads = 3;

$options = [
    'threads' => $threads,
    'time_cost' => $time_cost,
    'memory_cost' => $memory_cost,
];

$hashAllModifieded = @password_hash( $password, PASSWORD_ARGON2ID, $options);
$hash = @password_hash( $password, PASSWORD_ARGON2ID);

echo 'Verify hash with cost : ' . password_verify( $password, $hashAllModifieded) . "\n";
echo 'Verify hash without cost : ' . password_verify( $password, $hash) . "\n";
