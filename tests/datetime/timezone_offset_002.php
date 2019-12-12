<?php
namespace datetime\timezone_offset_002;

function test() {
	$dt = new \DateTime('2019-11-13 18:00:00', new \DateTimeZone('-05:00'));
	
	return $dt->getTimestamp();
}

echo test();

echo 'Done.';