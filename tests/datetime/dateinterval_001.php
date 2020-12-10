<?php
namespace date\dateinterval_001;

function test() {

    $now = new \DateTime();

    echo "Checking format with date_diff null".PHP_EOL;
    $interval = date_diff($now, $now);
    $interval->days = 5;
    echo gettype($interval->days) ."=". $interval->days .PHP_EOL;
    echo print_r($interval->format('%r%a'), true).PHP_EOL;
    echo print_r($interval->format('%r%d'), true).PHP_EOL;
    echo print_r($interval->format('%m month, %d days, %I mins'), true).PHP_EOL;

    echo "Checking format with date_diff negative".PHP_EOL;
    $interval = date_diff((clone $now)->add(new \DateInterval('P4D')), $now);
    echo gettype($interval->days) ."=". $interval->days .PHP_EOL;
    echo print_r($interval->format('%r%a'), true).PHP_EOL;
    echo print_r($interval->format('%r%d'), true).PHP_EOL;
    echo print_r($interval->format('%m month, %d days, %I mins'), true).PHP_EOL;
    echo PHP_EOL;

    echo "Checking format with date_diff positive".PHP_EOL;
    $interval = date_diff($now, (clone $now)->add(new \DateInterval('P2D')));
    echo gettype($interval->days) ."=". $interval->days .PHP_EOL;
    echo print_r($interval->format('%r%a'), true).PHP_EOL;
    echo print_r($interval->format('%r%d'), true).PHP_EOL;
    echo print_r($interval->format('%m month, %d days, %I mins'), true).PHP_EOL;
    echo PHP_EOL;

    echo "Checking format with date_diff complex".PHP_EOL;
    $interval = date_diff(\DateTime::createFromFormat('Ymd','20160423'), \DateTime::createFromFormat('Ymd H:i:s','20160402 15:16:17'));
    echo gettype($interval->days) ."=". $interval->days .PHP_EOL;
    echo print_r($interval->format('%r%a'), true).PHP_EOL;
    echo print_r($interval->format('%r%d'), true).PHP_EOL;
    echo print_r($interval->format('%m month, %d days, %I mins'), true).PHP_EOL;
    echo PHP_EOL;

    // TODO: .NET takes into account summer time and we internally compute everything in UTC,
    // so following is off of 1 hour, resulting in 194 days instead of 195

    // echo "Checking format with date_diff complex without time".PHP_EOL;
    // $interval = date_diff(\DateTime::createFromFormat('Ymd','20200101'), \DateTime::createFromFormat('Ymd','20200714'));
    // echo gettype($interval->days) ."=". $interval->days .PHP_EOL;
    // echo print_r($interval->format('%r%a'), true).PHP_EOL;
    // echo print_r($interval->format('%r%d'), true).PHP_EOL;
    // echo print_r($interval->format('%m month, %d days, %I mins'), true).PHP_EOL;
    // echo PHP_EOL;

    echo "Checking format with date_diff complex with time".PHP_EOL;
    $interval = date_diff(\DateTime::createFromFormat('Ymd H:i:s','20200101 09:16:52'), \DateTime::createFromFormat('Ymd','20200714'));
    echo gettype($interval->days) ."=". $interval->days .PHP_EOL;
    echo print_r($interval->format('%r%a'), true).PHP_EOL;
    //echo print_r($interval->format('%r%d'), true).PHP_EOL;
    //echo print_r($interval->format('%m month, %d days, %I mins'), true).PHP_EOL;
    echo PHP_EOL;
    
    echo "Checking format without date_diff = (unknown)".PHP_EOL;
    $interval = new \DateInterval('P2D');
    echo gettype($interval->days) ."=". $interval->days .PHP_EOL;
    echo print_r($interval->format('%r%a'), true).PHP_EOL;
    echo print_r($interval->format('%r%d'), true).PHP_EOL;
    echo print_r($interval->format('%m month, %d days, %I mins'), true).PHP_EOL;
    echo PHP_EOL;
}

test();
