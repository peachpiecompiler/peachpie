<?php

$data = array(
   'one',
   'two',
   'three',
   'four',
   'five',
   'six',
   'seven',
   'eight',
   'nine',
   'ten',
   );
$pdo = new \PDO("sqlite::memory:");
$pdo->exec("CREATE TABLE strings(a)");
$insert = $pdo->prepare('INSERT INTO strings VALUES (?)');
foreach ($data as $str) {
    $insert->execute(array($str));
}
$insert = null;

function max_len_step($context, $rownumber, $string)
{
    if (strlen($string) > $context) {
        $context = strlen($string);
    }
    return $context;
}

function max_len_finalize($context, $rownumber)
{
    return $context === null ? 0 : $context;
}

$pdo->sqliteCreateAggregate('max_len', max_len_step, max_len_finalize);
$stmt = $pdo->prepare('SELECT max_len(a) from strings');
$stmt->execute();
$result = $stmt->fetch(\PDO::FETCH_NUM)[0];
if ($result != 5) {
    throw new ErrorException("Expecting '5'");
}
