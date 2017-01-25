<?php 

// email validation and sanitization
$email_addresses = array("info@example.org", "a@b!@#$%^&*[].com", "a@456.com", "123@b.com", null, "");

foreach ($email_addresses as $email)
{
	echo "FILTER_SANITIZE_EMAIL:" . print_r(filter_var($email, FILTER_SANITIZE_EMAIL)) . "\n";
	echo "FILTER_VALIDATE_EMAIL:" . print_r(filter_var($email, FILTER_VALIDATE_EMAIL)) . "\n";
}
