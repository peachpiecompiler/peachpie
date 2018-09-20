<?php

function f() {
	preg_match_all(
		'#<(?P<tag>video|object|embed|iframe)[^<]*?(?:>[\s\S]*?<\/(?P=tag)>|\s*\/>)#',
		"<video>content</video>",
		$matches );

	print_r( $matches );
}

f();
