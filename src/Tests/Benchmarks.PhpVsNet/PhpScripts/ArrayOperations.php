<?php
// Array operations - PhpArray is the heart of PeachPie runtime
// Benchmarks array read/write, iteration, and manipulation

namespace BenchmarkPhp;

class ArrayOps
{
    public static function fillArray(int $size): array
    {
        $arr = [];
        for ($i = 0; $i < $size; $i++) {
            $arr[$i] = $i * 2;
        }
        return $arr;
    }

    public static function readArray(array $arr): int
    {
        $sum = 0;
        foreach ($arr as $key => $val) {
            $sum += $val;
        }
        return $sum;
    }

    public static function readArrayByIndex(array $arr): int
    {
        $sum = 0;
        $count = count($arr);
        for ($i = 0; $i < $count; $i++) {
            $sum += $arr[$i];
        }
        return $sum;
    }

    public static function writeArray(int $size): array
    {
        $arr = [];
        for ($i = 0; $i < $size; $i++) {
            $arr["key_$i"] = $i;
        }
        return $arr;
    }

    public static function mixedOperations(int $size): array
    {
        $arr = [];
        for ($i = 0; $i < $size; $i++) {
            $arr[] = $i;
            $arr["str_$i"] = "value_$i";
            $arr[$i * 2] = $i * 3;
        }
        return $arr;
    }

    public static function arrayFilter(array $arr): array
    {
        $result = [];
        foreach ($arr as $key => $val) {
            if ($val % 2 === 0) {
                $result[$key] = $val;
            }
        }
        return $result;
    }

    public static function arrayMap(array $arr): array
    {
        $result = [];
        foreach ($arr as $key => $val) {
            $result[$key] = $val * 2 + 1;
        }
        return $result;
    }
}
