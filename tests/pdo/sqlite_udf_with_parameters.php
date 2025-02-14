<?php

function php_add($left, $right) {
    $ret = $left + $right;
    return $ret;
}

$pdo = new \PDO("sqlite::memory:");
$pdo->sqliteCreateFunction( 'php_add', php_add );
$stmt = $pdo->prepare("SELECT php_add(1, 2)");
$stmt->execute();
$result = $stmt->fetch(\PDO::FETCH_NUM)[0];
if ($result != 3) {
    throw new ErrorException("Expecting '3'");
}