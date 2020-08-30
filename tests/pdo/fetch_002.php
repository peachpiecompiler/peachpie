<?php
namespace pdo\fetch_002;

class PdoFetchClassTest { 
    public $a; 
    public $b;
    public $notmapped;

    public function __construct($notmapped) {
        $this->notmapped = $notmapped;
    }
}

function test() {
    /* Testing PDO::FETCH_CLASS */
    $className = PdoFetchClassTest::class;

    $pdo = new \PDO("sqlite::memory:");

    $pdo->exec("CREATE TABLE test (a INTEGER, b INTEGER NULL)");
    $pdo->exec("INSERT INTO test VALUES (1, NULL)");
    $pdo->exec("INSERT INTO test VALUES (2, 3)");
    
    echo "Test PDO::FETCH_CLASS with setFetchMode + Fetch" . PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    $stmt->setFetchMode(\PDO::FETCH_CLASS, $className, array(42));
    print_r($stmt->fetch());
    
    echo "Test PDO::FETCH_CLASS with fetchAll" . PHP_EOL;
    $stmt = $pdo->prepare("SELECT * FROM test");
    $stmt->execute();
    print_r($stmt->fetchAll(\PDO::FETCH_CLASS, $className, array(74)));
}

test();
