<?php
namespace hash\password_needs_rehash_001;

$password = 'rasmuslerdorf';
$hash = '$2y$10$YCFsG6elYca568hBi2pZ0.3LDL5wjgxct1N8w/oLR/jfHsiQwCqTS';

// The cost parameter can change over time as hardware improves
$options = array('cost' => '11');

// Verify stored hash against plain-text password
if (password_verify($password, $hash)) {
    // Check if a newer hashing algorithm is available
    // or the cost has changed
    if (password_needs_rehash($hash, PASSWORD_DEFAULT, $options)) {
        // If so, create a new hash, and replace the old one
        $newHash = @password_hash($password, PASSWORD_DEFAULT, $options);
        echo 'Success';
    }
    else
    {
        echo 'Failed';
    }
    if (password_needs_rehash(null, PASSWORD_DEFAULT, $options)) {
        // If so, create a new hash, and replace the old one
        echo 'Success';
    }
    else
    {
        echo 'Failed';
    }
    // Log user in
}
