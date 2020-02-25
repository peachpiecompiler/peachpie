<?php

$encrypted = "9hzft/nQs7HTlazA8khf67wIBizBBEXCj5/VbSxcEuPMPb6pmB9bZeiFxc7eUsfU";
$cipher= "des-ecb";
$key="Fs9H083h5TtLdkAePgmgOxxIPEEptSOz59MzCR4sh5w=";
$iv="FuK2LSE8rnYPLU/P9Z7d2w==";

$ciphertext = @openssl_decrypt($encrypted, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "des-cbc";
$encrypted = "YNGhhYZopS4ac5SaENpICZR1wSJkcyL99Xda9091S9xYihPqeaI1HYxBpv0zmWw7";
$ciphertext = @openssl_decrypt($encrypted, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";