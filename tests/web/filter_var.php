<?php
namespace web\filter_var; 

// email validation and sanitization
$email_addresses = array("info@example.org", "a@b!@#$%^&*[].com", "a@456.com", "123@b.com", null, "");

foreach ($email_addresses as $email)
{
	echo "FILTER_SANITIZE_EMAIL:" . print_r(filter_var($email, FILTER_SANITIZE_EMAIL)) . "\n";
	echo "FILTER_VALIDATE_EMAIL:" . print_r(filter_var($email, FILTER_VALIDATE_EMAIL)) . "\n";
}

$ip_tests = [
    ["127.0.0.1", FILTER_VALIDATE_IP],
    ["invalid-ip", FILTER_VALIDATE_IP],
    ["127.0.0.1", FILTER_VALIDATE_IP, FILTER_FLAG_IPV4],
    ["::1", FILTER_VALIDATE_IP, FILTER_FLAG_IPV4],
    ["invalid-ip", FILTER_VALIDATE_IP, FILTER_FLAG_IPV4],
    ["127.0.0.1", FILTER_VALIDATE_IP, FILTER_FLAG_IPV6],
    ["::1", FILTER_VALIDATE_IP, FILTER_FLAG_IPV6],
    ["invalid-ip", FILTER_VALIDATE_IP, FILTER_FLAG_IPV6],
	
    ["/test.php", FILTER_VALIDATE_URL],
    ["http://localhost/test.php", FILTER_VALIDATE_URL],
    ["http:///test.php", FILTER_VALIDATE_URL],
];

foreach ($ip_tests as $index => $test) {
	echo "{$index} => ";
	
    $result = filter_var($test[0], $test[1], $test[2] ?? null);

	echo $result ? "{$result}" : "false";
	echo "\n";
}

// FILTER_VALIDATE_INT:
// https://github.com/peachpiecompiler/peachpie/issues/661

echo ",500:", filter_var(500, FILTER_VALIDATE_INT);
echo ",500.0:", filter_var(500.0, FILTER_VALIDATE_INT);
echo ",true:", filter_var(true, FILTER_VALIDATE_INT);
echo ",'500'", filter_var('500', FILTER_VALIDATE_INT);
echo ",'500.0:'", filter_var("500.0", FILTER_VALIDATE_INT);
echo ",[]:", filter_var([], FILTER_VALIDATE_INT);
echo ",null:", filter_var(null, FILTER_VALIDATE_INT);

// FILTER_SANITIZE_STRING
class X
{
    function __toString() {
        return __CLASS__;
    }
}

// FILTER_SANITIZE_STRING
foreach ([null, 123, true, "Simple String", "With <a> tag", new X, [1, 2, 3]] as $ord => $value)
{
    echo "FILTER_SANITIZE_STRING ({$ord}) = ";
    print_r(filter_var($value, FILTER_SANITIZE_STRING));
    echo PHP_EOL;
}
