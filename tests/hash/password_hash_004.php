<?php
namespace hash\password_hash_004;
/**Test PASSWORD_ARGON2ID with options. */
$password = "rasmuslerdorf";
$memory_cost = 512;
$time_cost = 4.3;
$threads = "1";

$options = [
    'threads' => $threads,
    'time_cost' => $time_cost,
    'memory_cost' => $memory_cost,
];

$hashAllModifieded = @password_hash( $password, PASSWORD_ARGON2ID, $options);
$hash = @password_hash( $password, PASSWORD_ARGON2ID);

echo 'Verify hash with cost : ' . password_verify( $password, $hashAllModifieded) . "\n";
echo 'Verify hash without cost : ' . password_verify( $password, $hash) . "\n";
echo 'Verify with null parameters : ' . password_verify( $password, null) . "\n";
echo 'Verify with null parameters : ' . password_verify( null, null) . "\n";
echo 'Verify with null parameters : ' . password_verify( "", "") . "\n";