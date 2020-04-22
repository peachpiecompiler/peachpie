<?php
namespace pdo\fetch_001;

function test() {
    $pdo = new \PDO("sqlite::memory:");

    // PDO::ATTR_STRINGIFY_FETCHES is not honored by sqlite,
    // although PDO always returns TRUE
    $success = $pdo->setAttribute(\PDO::ATTR_STRINGIFY_FETCHES, false);
    echo "stringify set: ", $success ? "yes" : "no", PHP_EOL;

    // Sqlite does not handle this attribute,
    // it always returns FALSE even the driver actually stringifies all the values
    //$success = $pdo->getAttribute(\PDO::ATTR_STRINGIFY_FETCHES);
    //echo "stringify: ", $success ? "yes" : "no", PHP_EOL;

    $pdo->exec("CREATE TABLE test (n INTEGER NULL, i INTEGER, r REAL, t TEXT, b BLOB)");
    $pdo->exec("INSERT INTO test VALUES (NULL, 42, 3.14, 'Lorem Ipsum', 'Dolor sit amet')");

    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();

    foreach ($stmt->fetch(\PDO::FETCH_ASSOC) as $val) {
        echo "{$val}:", gettype($val), PHP_EOL;
    }
}

test();
