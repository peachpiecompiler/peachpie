<?php
namespace functions\factorial;

/**
 * @param int $number
 */
function factorial($number)
{
    if ($number < 2) {
        return 1;
    } else {
        return factorial($number - 1) * $number;
    }
}

echo factorial(4), ', ',
	 factorial(8), ', ',
	 factorial(16);
