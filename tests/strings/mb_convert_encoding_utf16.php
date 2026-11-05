<?php
namespace strings\mb_convert_encoding_utf16;

$utf8_str = "这是一个测试";

// UTF-16LE conversion should match iconv
$mb_utf16 = mb_convert_encoding($utf8_str, 'UTF-16LE', 'UTF-8');
$expected = iconv('UTF-8', 'UTF-16LE', $utf8_str);
echo base64_encode($mb_utf16) . "\n";
echo base64_encode($expected) . "\n";

// UTF-16BE conversion should match iconv
$mb_utf16be = mb_convert_encoding($utf8_str, 'UTF-16BE', 'UTF-8');
$expected_be = iconv('UTF-8', 'UTF-16BE', $utf8_str);
echo base64_encode($mb_utf16be) . "\n";
echo base64_encode($expected_be) . "\n";

// Round-trip: UTF-16LE -> UTF-8
$back = mb_convert_encoding($mb_utf16, 'UTF-8', 'UTF-16LE');
echo $back . "\n";
echo $utf8_str . "\n";

// ASCII conversion
$mb_ascii = mb_convert_encoding("Hello World", 'ASCII', 'UTF-8');
echo base64_encode($mb_ascii) . "\n";

// mb_strlen on UTF-16LE should return character count
echo mb_strlen($mb_utf16, 'UTF-16LE') . "\n";
echo mb_strlen($utf8_str, 'UTF-8') . "\n";

// mb_strlen 8bit should return byte count
echo mb_strlen(random_bytes(16), '8bit') . "\n";

// mb_strpos on UTF-16LE
$haystack = mb_convert_encoding("这是一个测试", 'UTF-16LE', 'UTF-8');
$needle = mb_convert_encoding("测试", 'UTF-16LE', 'UTF-8');
echo mb_strpos($haystack, $needle, 0, 'UTF-16LE') . "\n";

echo "Done.\n";