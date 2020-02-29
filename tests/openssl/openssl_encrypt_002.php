<?php
 $password = "Fs9H083h5Tt";
 $cipher = 'des-cbc';
 $iv="FuK2LSE8rnYPLU/P9Z7d2w==";
 $textToCrypt = 'The plaintext message data to be encrypted.';

 $encrypted = @openssl_encrypt($textToCrypt, $cipher, $password,0,  $iv);
 echo $cipher . " : " . $encrypted ."\n";

 $cipher = 'des-ecb';
 $encrypted = @openssl_encrypt($textToCrypt, $cipher, $password,0,  $iv);
 echo $cipher . " : " . $encrypted ."\n";