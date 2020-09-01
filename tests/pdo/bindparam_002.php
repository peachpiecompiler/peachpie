<?php

namespace pdo\bindparam_002;

function test()
{
	$pdo = new \PDO("sqlite::memory:");
	$pdo->exec("PRAGMA foreign_keys = ON;");
	$pdo->exec("CREATE TABLE posts (id INTEGER PRIMARY KEY)");
	$pdo->exec("INSERT INTO posts (id) VALUES (null), (null), (null), (null)");
	$pdo->exec("CREATE TABLE other (id INTEGER PRIMARY KEY, post_id INTEGER, FOREIGN KEY(post_id) REFERENCES posts(id))");
	$pdo->exec("INSERT INTO other (post_id) VALUES (1), (1), (2)");

	$stmt = $pdo->prepare("UPDATE other SET post_id=:post_id WHERE id=1");
	$stmt->execute(array(":post_id" => 2));

	$stmt = $pdo->prepare("SELECT * FROM other");
	$stmt->execute();
	print_r($stmt->fetchAll(\PDO::FETCH_ASSOC));

	$stmt = $pdo->prepare("UPDATE other SET post_id=:post_id WHERE id=2");
	$stmt->execute(array(":post_id" => NULL));

	$stmt = $pdo->prepare("SELECT * FROM other");
	$stmt->execute();
	print_r($stmt->fetchAll(\PDO::FETCH_ASSOC));
}

test();

echo "Done.";
