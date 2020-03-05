<?php
namespace hash\password_get_info_001;

$password = "rasmuslerdorf";
$salt = "Ajnbu298IRHUVa56XvDOzu";
$memory_cost = 512;
$time_cost = 10;
$threads = 6;

$options = [
    'threads' => $threads,
    'time_cost' => $time_cost,
    'memory_cost' => $memory_cost,
    'cost' => $time_cost,
    'salt' => $salt,
];

$hashBCrypt = @\password_hash($password, \PASSWORD_DEFAULT,$options);
$hashArgon2ID = @\password_hash( $password, \PASSWORD_ARGON2ID, $options);
$hashArgon2I= @\password_hash( $password, \PASSWORD_ARGON2I, $options);

print_r(password_get_info(NULL));
print_r(password_get_info("UnknownAlgorithm"));
print_r(password_get_info($hashBCrypt));
print_r(password_get_info($hashArgon2ID));
print_r(password_get_info($hashArgon2I));

echo "Done.";
