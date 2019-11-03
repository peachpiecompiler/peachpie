<?php
namespace classes\ctor_default_param;

class X {
	function __construct(array $data = []) {
		print_r($data);
	}
}

new X();
new X([1, 2, 3]);
