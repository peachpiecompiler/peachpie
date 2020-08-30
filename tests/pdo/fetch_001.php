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
    $pdo->exec("INSERT INTO test VALUES (1, 74, 3.14, 'Foo', 'Bar')");
    
    echo "Test with fetch with assoc".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetch(\PDO::FETCH_ASSOC));

    echo "Test with fetchAll with assoc".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll(\PDO::FETCH_ASSOC));
    
    echo "Test with fetch with num".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetch(\PDO::FETCH_NUM));

    echo "Test with fetchAll with num".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll(\PDO::FETCH_NUM));
    
    echo "Test with fetch with both".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetch(\PDO::FETCH_BOTH));

    echo "Test with fetchAll with both".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll(\PDO::FETCH_BOTH));
    
    echo "Test with fetch with both (default)".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetch());

    echo "Test with fetchAll with both (default)".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll());
}

test();
