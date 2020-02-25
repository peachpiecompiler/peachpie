<?php
$text = "";
$raw = false;

$supportedMethods = array("md4", "md5", "sha256", "sha512", "sha384", "sha1");

foreach ($supportedMethods as $method)
{
    $hash = openssl_digest($text, $method, $raw);
    echo "" . $method . " : " . $hash . "\n";
}

$raw = true;

foreach ($supportedMethods as $method)
{
    $hash = openssl_digest($text, $method, $raw);
    echo "" . $method . " : " . $hash . "\n";
}