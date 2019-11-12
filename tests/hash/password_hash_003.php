<?php
namespace hash\password_hash_003;
/**Test PASSWORD_ARGON2I with options. */
$password = "rasmuslerdorf";
$memory_cost = 512;
$time_cost = 4;
$threads = 1;

$options = [
    'threads' => $threads,
    'time_cost' => $time_cost,
    'memory_cost' => $memory_cost,
];
                                               
$hashAllModifieded = @password_hash( $password, PASSWORD_ARGON2I, $options);
$hash = @password_hash( $password, PASSWORD_ARGON2I);

echo 'Verify hash with cost : ' . password_verify( $password, $hashAllModifieded) . "\n";
echo 'Verify hash without cost : ' . password_verify( $password, $hash) . "\n";
