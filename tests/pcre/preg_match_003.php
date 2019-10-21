<?php
namespace pcre\preg_match_003;

function f($iri) {
	preg_match('/^((?P<scheme>[^:\/?#]+):)?(\/\/(?P<authority>[^\/?#]*))?(?P<path>[^?#]*)(\?(?P<query>[^#]*))?(#(?P<fragment>.*))?$/', $iri, $matches);
	print_r($matches);
}

f('http://www.example.org/');
f('http://www.example.org/path/file.php');
f('http://www.example.org/path/file.php?query1=1&query2=2');
f('https://www.example.org:8443/path/file.php?query1=1&query2=2');
f('https://www.example.org:8443/path/file.php?query1=1&query2=2#fragment');
f('https://www.example.org:8443#fragment');
f('https://www.example.org:8443/path/file.php#fragment');
f('https://www.example.org:8443?query1=1&query2=2');

echo "Done";
