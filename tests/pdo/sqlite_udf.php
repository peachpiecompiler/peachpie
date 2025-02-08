<?php

function foo() {
    return 'Hello World!';
}

$pdo = new \PDO("sqlite::memory:");
$pdo->sqliteCreateFunction( 'foo', foo );
$stmt = $pdo->prepare("SELECT foo()");
$stmt->execute();
$result = $stmt->fetch(\PDO::FETCH_NUM)[0];
if ($result != foo()) {
    throw new ErrorException("Expecting 'Hello World'");
}