<?php
namespace pdo\fetch_003;

function test() {
    /* Testing PDO::FETCH_GROUP */
    $pdo = new \PDO("sqlite::memory:");

    $pdo->exec("CREATE TABLE test (a INTEGER, n INTEGER NULL, i INTEGER, r REAL, t TEXT, b BLOB)");
    $pdo->exec("INSERT INTO test VALUES (1, NULL, 42, 3.14, 'Lorem Ipsum', 'Dolor sit amet')");
    $pdo->exec("INSERT INTO test VALUES (2, NULL, 42, 3.14, 'Lorem Ipsum', 'Dolor sit amet')");
    $pdo->exec("INSERT INTO test VALUES (1, 2, 74, 3.1425, 'Foo', 'Bar')");

    echo "Test with Fetch with assoc & group".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetch(\PDO::FETCH_ASSOC | \PDO::FETCH_GROUP));
    
    echo "Test with FetchAll with assoc & group".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll(\PDO::FETCH_ASSOC | \PDO::FETCH_GROUP));
    
    echo "Test with Fetch with num & group".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetch(\PDO::FETCH_NUM | \PDO::FETCH_GROUP));
    
    echo "Test with FetchAll with num & group".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll(\PDO::FETCH_NUM | \PDO::FETCH_GROUP));

    echo "Test with Fetch with both (default) & group".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetch(\PDO::FETCH_GROUP));
    
    echo "Test with FetchAll with both (default) & group".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll(\PDO::FETCH_GROUP));
    
    echo "Test with Fetch with both & group".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetch(\PDO::FETCH_BOTH | \PDO::FETCH_GROUP));
    
    echo "Test with FetchAll with both & group".PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll(\PDO::FETCH_BOTH | \PDO::FETCH_GROUP));
}

test();
