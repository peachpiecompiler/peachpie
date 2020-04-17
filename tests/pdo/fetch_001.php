<?php
namespace pdo\fetch_001;

function test() {
  $pdo = new \PDO("sqlite::memory:");
  $pdo->exec("CREATE TABLE test (n INTEGER NULL, i INTEGER, r REAL, t TEXT, b BLOB)");
  $pdo->exec("INSERT INTO test VALUES (NULL, 42, 3.14, 'Lorem Ipsum', 'Dolor sit amet')");

  $stmt = $pdo->prepare("SELECT * FROM test");
	$stmt->execute();
  foreach ($stmt->fetch(\PDO::FETCH_ASSOC) as $val) {
    echo "{$val}:". gettype($val) ."\n";
  }
}

test();
