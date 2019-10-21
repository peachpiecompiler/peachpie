<?php
namespace functions\leibnitz;

/**
 * @param int $accuracy
 */
function pi_leibnitz($accuracy)
{
	$pi = 4.0; $top = 4.0; $bot = 3; $minus = TRUE;
	for($i = 0; $i < $accuracy; $i++)
	{
		$pi += ( $minus ? -($top/$bot) : ($top/$bot) );
		$minus = ( $minus ? FALSE : TRUE); 
		$bot += 2;
	}
	return $pi;
}

echo (int)pi_leibnitz(100);	// convert to int, since pchp is more precise than php