<?php
namespace hash\password_hash_002;
/**Test PASSWORD_BCRYPT  Verify. */
$password = "rasmuslerdorf";
$cost = 9;
$options = [
    'cost' => $cost,
];
$hashWithCost = @password_hash($password, PASSWORD_BCRYPT, $options);
$hashWithoutCost =  @password_hash($password, PASSWORD_BCRYPT);

echo 'Verify hash with cost : ' . password_verify($password,$hashWithCost) . "\n";
echo 'Verify hash without cost : ' . password_verify($password,$hashWithoutCost) . "\n";
