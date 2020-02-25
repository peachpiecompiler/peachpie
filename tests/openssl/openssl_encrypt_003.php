<?php
 $password = "Fs9H083h5Ttaaaaaaaaaaaa";
 $iv="FuK2LSE8rnYPLU/P9Z7d2w==";
 $textToCrypt = 'The plaintext message data to be encrypted.';

//  $cipher = 'des-ede';
//  $encrypted = @openssl_encrypt($textToCrypt, $cipher, $password,0,  $iv);
//  echo $cipher . " : " . $encrypted ."\n";

//  $cipher = 'des-ede-cbc';
//  $encrypted = @openssl_encrypt($textToCrypt, $cipher, $password,0,  $iv);
//  echo $cipher . " : " . $encrypted ."\n";
 
 $cipher = 'des-ede3';
 $encrypted = @openssl_encrypt($textToCrypt, $cipher, $password,0,  $iv);
 echo $cipher . " : " . $encrypted ."\n";
 
 $cipher = 'des-ede3-cbc';
 $encrypted = @openssl_encrypt($textToCrypt, $cipher, $password,0,  $iv);
 echo $cipher . " : " . $encrypted ."\n";