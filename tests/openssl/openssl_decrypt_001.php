<?php

$encrypted = "TggEvp4mQoFKjulqwbtZoC31elXF/Tsjk4jK6GeWpsJbJNfn7FkVTfJynyjIt59S";
$cipher= "aes-256-cbc";
$key="Fs9H083h5TtLdkAePgmgOxxIPEEptSOz59MzCR4sh5w=";
$iv="FuK2LSE8rnYPLU/P9Z7d2w==";

$ciphertext = @openssl_decrypt($encrypted , $cipher, $key, 0, base64_decode($iv));
echo $cipher . " : " . $ciphertext ."\n";

$cipher= "aes-192-cbc";
$encrypted = "S2Uc5lUHhcPE7+4L+WFZeFNbhLEmWw0SwcTtBI26HtPBp+uHLiaM9AHWzmnpceRA";

$ciphertext = @openssl_decrypt($encrypted , $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "aes-128-cbc";
$encrypted = "MJH2DwCJxOYU8dHydTZ+AzOR9MoyIr/nlBOx+oMkG5Baqgwfc1pA6ZlRkpGWfliF";

$ciphertext = @openssl_decrypt($encrypted , $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "aes-256-ecb";
$encrypted = "K93+IDFZxoC3vpDQeVsMC902Yt/eBBjaq/6LET6/PztoiZr/pzOgaaesaTY8bagE";

$ciphertext = @openssl_decrypt($encrypted , $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "aes-192-ecb";
$encrypted = "ZMdNPoD/xq8HE/3efJ/V10pptQRNwDEz2oscVNQunnXbgNUqIbe/tL26Uh9MK78t";

$ciphertext = @openssl_decrypt($encrypted , $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";

$cipher= "aes-128-ecb";
$encrypted = "67CmNRreDbJuIHzeBpZKrltRffUG/ym8Vwyq0V6PP3Yd8cux4/5ivbpG0GcE2uM6";

$ciphertext = @openssl_decrypt($encrypted , $cipher, $key, 0, $iv);
echo $cipher . " : " . $ciphertext."\n";