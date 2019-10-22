<?php
namespace pcre\preg_match_002;

function f() {
	// The "i" after the pattern delimiter indicates a case-insensitive search
	if (preg_match("/php/i", "PHP is the web scripting language of choice.")) {
		echo "A match was found.";
	} else {
		echo "A match was not found.";
	}

	/* The \b in the pattern indicates a word boundary, so only the distinct
	 * word "web" is matched, and not a word partial like "webbing" or "cobweb" */
	if (preg_match("/\bweb\b/i", "PHP is the web scripting language of choice.")) {
		echo "A match was found.";
	} else {
		echo "A match was not found.";
	}

	if (preg_match("/\bweb\b/i", "PHP is the website scripting language of choice.")) {
		echo "A match was found.";
	} else {
		echo "A match was not found.";
	}

	$str = 'foobar: 2008';

	preg_match('/(?P<name>\w+): (?P<digit>\d+)/', $str, $matches);
	print_r($matches);

	/* This also works in PHP 5.2.2 (PCRE 7.0) and later, however 
	 * the above form is recommended for backwards compatibility */
	preg_match('/(?<name>\w+): (?<digit>\d+)/', $str, $matches);
	print_r($matches);

	// get host name from URL
	preg_match('@^(?:http://)?([^/]+)@i', "http://www.php.net/index.html", $matches);
	$host = $matches[1];

	// get last two segments of host name
	preg_match('/[^.]+\.[^.]+$/', $host, $matches);
	echo "domain name is: {$matches[0]}\n";

    // escaped regular letter // https://github.com/peachpiecompiler/peachpie/issues/393
    preg_match('/\_/', $str);
}

f();