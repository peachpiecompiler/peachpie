<?php

$encrypted = "50NR3EsmTMZBd8meqVOVsCPo5CfYoFlxhjBsIwUUQ+fLYCVRGzyvr9s9jkF0Z99W";
$cipher= "des-ede3";
$key="Fs9H083h5Ttaaaaaaaaaaaa";
$iv="FuK2LSE8rnYPLU/P9Z7d2w==";

$ciphertext = @openssl_decrypt($encrypted, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "des-ede3-cbc";
$encrypted = "P5YRY+nQsZNOISQxoZB0+tJuEIEYQUkQVSkSNV0tlgZat7Az7IInjfKY5GVTmQZo";
$ciphertext = @openssl_decrypt($encrypted, $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";