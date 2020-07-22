<?php
namespace datetime\set_timezone_001;

function test()
{
  date_default_timezone_set('UTC');

  $dt = date_create('2019-04-25 02:59:00');
  $dt->setTimezone(new \DateTimeZone('Europe/Kiev'));
  echo $dt->format('Y-m-d H:i:s');
  echo "\n";

  $dt = date_create('2019-04-25 02:59:00');
  $dt->setTimezone(new \DateTimeZone('America/New_York'));
  echo $dt->format('Y-m-d H:i:s');
}

test();
