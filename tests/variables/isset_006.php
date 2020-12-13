<?php
namespace variables\isset_006;

class Test
{
    // https://github.com/peachpiecompiler/peachpie/issues/887
    final function multidimensional( $root, $keys ) {
        if (empty($root)) {
            return; // void
        }
        else {
            return [1,2,3];
        }
    }

    function multidimensional_get( $root, $keys, $default = null ) {
		$result = $this->multidimensional( $root, $keys );
		return isset( $result ) ? $result['node'] : $default;
	}
}
echo (new Test)->multidimensional_get(false, false, "ok");