<?php

namespace pdo\stmtclass;

class CustomStatement extends \PDOStatement
{
    protected function __construct()
    {
    }
}

function stmtclass()
{
	// https://github.com/peachpiecompiler/peachpie/issues/549
	$pdo = new \PDO("sqlite::memory:");
	$pdo->setAttribute(\PDO::ATTR_STATEMENT_CLASS, [CustomStatement::class, []]);

	$stmt = $pdo->prepare("SELECT name FROM sqlite_master");

	echo \get_class($stmt), PHP_EOL;
}

stmtclass();

echo "Done.";
