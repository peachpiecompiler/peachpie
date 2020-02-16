<?php

$key="Fs9H083h5TtLdkAePgmgOxxIPEEptSOz59MzCR4sh5w=";
$iv="FuK2LSE8rnYPLU/P9Z7d2w==";

$supportedMethods = array(
    "aes-192-cbc" =>  "S2Uc5lUHhcPE7+4L+WFZeFNbhLEmWw0SwcTtBI26HtPBp+uHLiaM9AHWzmnpceRA",
    "aes-128-cbc" => "MJH2DwCJxOYU8dHydTZ+AzOR9MoyIr/nlBOx+oMkG5Baqgwfc1pA6ZlRkpGWfliF",
    "aes-256-ecb" => "K93+IDFZxoC3vpDQeVsMC902Yt/eBBjaq/6LET6/PztoiZr/pzOgaaesaTY8bagE",
    "aes-192-ecb" => "ZMdNPoD/xq8HE/3efJ/V10pptQRNwDEz2oscVNQunnXbgNUqIbe/tL26Uh9MK78t", 
    "aes-128-ecb" => "67CmNRreDbJuIHzeBpZKrltRffUG/ym8Vwyq0V6PP3Yd8cux4/5ivbpG0GcE2uM6"
);

$cipher= "aes-256-cbc";
$encrypted = "TggEvp4mQoFKjulqwbtZoC31elXF/Tsjk4jK6GeWpsJbJNfn7FkVTfJynyjIt59S";
$ciphertext = @openssl_decrypt($encrypted , $cipher, $key, 0, base64_decode($iv));
echo $cipher . " : " . $ciphertext ."\n";

foreach ($supportedMethods as $cipher => $encrypted)
{
    $ciphertext = @openssl_decrypt($encrypted , $cipher, $key, 0, $iv);
    echo $cipher . " : " . $ciphertext."\n";
}