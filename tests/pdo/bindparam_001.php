<?php

namespace pdo\bindparam_001;

function test()
{
	// https://github.com/peachpiecompiler/peachpie/issues/529
	$pdo = new \PDO("sqlite::memory:");
	$pdo->exec("CREATE TABLE posts (id INTEGER PRIMARY KEY)");
	$pdo->exec("INSERT INTO posts (id) VALUES (null), (null), (null), (null)");

	$value = null;
	$stmt = $pdo->prepare("SELECT * FROM posts WHERE id != ?");
	$stmt->bindParam(1, $value, \PDO::PARAM_STR);
	$stmt->execute();

	print_r($stmt->fetchAll());
}

test();

echo "Done.";
