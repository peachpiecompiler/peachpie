<?php
namespace datetime\timezone_offset_001;

function test() {	
	$dt = new \DateTime('2019-11-13 18:00:00', new \DateTimeZone('+0200'));
	
	return $dt->getTimestamp();
}

echo test();

echo 'Done.';