<?php
// String operations - concatenation, substring, formatting
// PeachPie uses PhpString which maps to .NET string with optimizations

namespace BenchmarkPhp;

class StringOps
{
    public static function concatStrings(int $count): string
    {
        $result = "";
        for ($i = 0; $i < $count; $i++) {
            $result .= "item_$i";
        }
        return $result;
    }

    public static function substrInLoop(string $str, int $count): string
    {
        $result = "";
        $len = strlen($str);
        for ($i = 0; $i < $count; $i++) {
            $result .= substr($str, $i % $len, 1);
        }
        return $result;
    }

    public static function repeatString(string $str, int $count): string
    {
        return str_repeat($str, $count);
    }

    public static function explodeImplode(string $delim, string $str): string
    {
        $parts = explode($delim, $str);
        return implode($delim, $parts);
    }

    public static function sprintfLoop(string $format, int $count): string
    {
        $result = "";
        for ($i = 0; $i < $count; $i++) {
            $result .= sprintf($format, $i);
        }
        return $result;
    }
}
