<?php
// Function call overhead - benchmarks direct calls vs indirect calls
// PeachPie compiles PHP functions to .NET methods, enabling JIT inlining

namespace BenchmarkPhp;

function simpleAdd(int $a, int $b): int
{
    return $a + $b;
}

function simpleMultiply(int $a, int $b): int
{
    return $a * $b;
}

function nestedCall(int $x): int
{
    return simpleAdd(simpleMultiply($x, 2), simpleMultiply($x, 3));
}

function deepChain(int $x): int
{
    $a = simpleAdd($x, 1);
    $b = simpleMultiply($a, 2);
    $c = simpleAdd($b, 3);
    $d = simpleMultiply($c, 4);
    return $d;
}

function variadicSum(...$args): int
{
    $sum = 0;
    foreach ($args as $arg) {
        $sum += $arg;
    }
    return $sum;
}

function recursiveFactorial(int $n): int
{
    if ($n <= 1) {
        return 1;
    }
    return $n * recursiveFactorial($n - 1);
}

class FunctionCallBench
{
    public static function directCall(int $iterations): int
    {
        $sum = 0;
        for ($i = 0; $i < $iterations; $i++) {
            $sum += simpleAdd($i, $i + 1);
        }
        return $sum;
    }

    public static function nestedCalls(int $iterations): int
    {
        $sum = 0;
        for ($i = 0; $i < $iterations; $i++) {
            $sum += nestedCall($i);
        }
        return $sum;
    }

    public static function deepChains(int $iterations): int
    {
        $sum = 0;
        for ($i = 0; $i < $iterations; $i++) {
            $sum += deepChain($i);
        }
        return $sum;
    }

    public static function recursiveFactorials(int $n): int
    {
        $sum = 0;
        for ($i = 1; $i <= $n; $i++) {
            $sum += recursiveFactorial($i);
        }
        return $sum;
    }
}
