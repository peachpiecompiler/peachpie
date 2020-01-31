<?php

namespace pdo\errmode;

function errmode()
{
	// https://github.com/peachpiecompiler/peachpie/issues/561
	$pdo = new \PDO("sqlite::memory:");
	$pdo->setAttribute(\PDO::ATTR_ERRMODE, \PDO::ERRMODE_EXCEPTION);
}

errmode();

echo "Done.";