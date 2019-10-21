<?php
namespace pcre\preg_replace_callback_001;

function f() {
	echo preg_replace_callback('~-([a-z])~', function ($match) {
		return strtoupper($match[1]);
	}, 'hello-world');
}

f();
