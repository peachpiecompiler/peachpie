<?php
 $password = 'Ty63rs4aVqcnh2vUqRJTbNT26caRZJ';
 $method = 'aes-256-cbc';
 $texteACrypter = 'The following example shows how to use the using statement.';

 $encrypted = @openssl_encrypt($texteACrypter, $method, $password, 1);
 echo "Encrypted : " . $encrypted."\n";

 $ciphertext = @openssl_decrypt($encrypted , $method, $password, 1);
 echo "Decrypted : " . $ciphertext ;