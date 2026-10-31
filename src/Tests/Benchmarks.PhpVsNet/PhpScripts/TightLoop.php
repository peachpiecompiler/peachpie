<?php
// Tight numeric loop - ideal for .NET JIT optimization (inlining, register allocation, SIMD)
// This is where compiled PHP on .NET should significantly outperform interpreted PHP

namespace BenchmarkPhp;

class TightLoop
{
    public static function sumInts(int $n): int
    {
        $sum = 0;
        for ($i = 0; $i < $n; $i++) {
            $sum += $i;
        }
        return $sum;
    }

    public static function sumIntsUnrolled(int $n): int
    {
        $sum = 0;
        $i = 0;
        for (; $i + 3 < $n; $i += 4) {
            $sum += $i;
            $sum += ($i + 1);
            $sum += ($i + 2);
            $sum += ($i + 3);
        }
        for (; $i < $n; $i++) {
            $sum += $i;
        }
        return $sum;
    }

    public static function multiplyMatrix(int $size): array
    {
        $result = [];
        for ($i = 0; $i < $size; $i++) {
            $row = [];
            for ($j = 0; $j < $size; $j++) {
                $val = 0;
                for ($k = 0; $k < $size; $k++) {
                    $val += $i * $k + $j;
                }
                $row[] = $val;
            }
            $result[] = $row;
        }
        return $result;
    }

    public static function fibonacciIterative(int $n): int
    {
        if ($n <= 1) {
            return $n;
        }
        $a = 0;
        $b = 1;
        for ($i = 2; $i <= $n; $i++) {
            $temp = $a + $b;
            $a = $b;
            $b = $temp;
        }
        return $b;
    }
}
