<?php

function f() {
	print_r( preg_split( '/(<.*>)/U', "<a>hello</a>", -1, PREG_SPLIT_DELIM_CAPTURE ) );
}

f();
