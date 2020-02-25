<?php

$plaintext = "The plaintext message data to be encrypted.";
$key="Fs9H083h5TtLdkAePgmgOxxIPEEptSOz59MzCR4sh5w=";
$iv="FuK2LSE8rnYPLU/P9Z7d2w==";

$supportedMethods = array("aes-256-cbc", "aes-192-cbc", "aes-128-cbc", "aes-256-ecb", "aes-192-ecb", "aes-128-ecb");

foreach ($supportedMethods as $cipher)
{
    $ciphertext = @openssl_encrypt($plaintext, $cipher, $key, 0, $iv);
    echo $cipher . " : " . $ciphertext ."\n";
}

// Not supported in .Net Core yet
// "aes-256-cfb", "aes-192-cfb", "aes-128-cfb" 