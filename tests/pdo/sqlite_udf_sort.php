<?php


$pdo = new \PDO("sqlite::memory:");
$pdo->exec("CREATE TABLE test (col1 string)");
$pdo->exec("INSERT INTO test VALUES ('a1')");
$pdo->exec("INSERT INTO test VALUES ('a10')");
$pdo->exec("INSERT INTO test VALUES ('a2')");

$pdo->sqliteCreateCollation('NATURAL_CMP', strnatcmp);


echo "default:\n";
foreach ($pdo->query("SELECT col1 FROM test ORDER BY col1") as $row){
    echo $row['col1'], "\n";
}

echo "\nnatural:\n";
foreach ($pdo->query("SELECT col1 FROM test ORDER BY col1 COLLATE NATURAL_CMP") as $row){
    echo $row['col1'], "\n";
}
