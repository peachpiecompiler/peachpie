<?php

$plaintext = "The plaintext message data to be encrypted.";
$cipher= "aes-256-cbc";
$key="Fs9H083h5TtLdkAePgmgOxxIPEEptSOz59MzCR4sh5w=";
$iv="FuK2LSE8rnYPLU/P9Z7d2w==";

$ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, base64_decode($iv));
echo $cipher . " : " . $ciphertext ."\n";

$cipher= "aes-192-cbc";

$ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "aes-128-cbc";

$ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "aes-256-ecb";

$ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "aes-192-ecb";

$ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "aes-128-ecb";

$ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

// Not supported in .Net Core yet
// $cipher= "aes-256-cfb"; 

// $ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
// echo $cipher . " : " . $ciphertext."\n";

// $cipher= "aes-192-cfb";

// $ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
// echo $cipher . " : " . $ciphertext."\n";

// $cipher= "aes-128-cfb";
// $ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
// echo $cipher . " : " . $ciphertext."\n";