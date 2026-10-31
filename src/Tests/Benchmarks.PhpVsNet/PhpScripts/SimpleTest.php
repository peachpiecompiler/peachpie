<?php
namespace BenchmarkPhp;

class SimpleTest
{
    public static function sumInts(int $n): int
    {
        $sum = 0;
        for ($i = 0; $i < $n; $i++) {
            $sum += $i;
        }
        return $sum;
    }
}
