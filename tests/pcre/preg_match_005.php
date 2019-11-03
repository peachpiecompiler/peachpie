<?php
namespace pcre\preg_match_005;

function f() {
	preg_match_all(
		'#<(?P<tag>video|object|embed|iframe)[^<]*?(?:>[\s\S]*?<\/(?P=tag)>|\s*\/>)#',
		"<video>content</video>",
		$matches );

	print_r( $matches );
}

f();
