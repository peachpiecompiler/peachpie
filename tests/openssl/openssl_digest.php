<?php
$text = "";
$raw = false;

$method = "md4";
$hash = openssl_digest($text, $method, $raw);
echo "" . $method . " : " . $hash . "\n";

$method = "md5";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . $hash . "\n";

$method = "sha256";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . $hash . "\n";

$method = "sha512";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . $hash . "\n";

$method = "sha384";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . $hash . "\n";

$method = "sha1";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . $hash . "\n";

$raw = true;

$method = "md4";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . base64_encode($hash) . "\n";

$method = "md5";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . base64_encode($hash) . "\n";

$method = "sha256";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . base64_encode($hash) . "\n";

$method = "sha512";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . base64_encode($hash) . "\n";

$method = "sha384";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . base64_encode($hash) . "\n";

$method = "sha1";
$hash = openssl_digest($text, $method, $raw);
echo $method . " : " . base64_encode($hash) . "\n";