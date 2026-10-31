<?php
// Object-oriented patterns - virtual method dispatch, inlining, JIT devirtualization
// PeachPie compiles PHP classes to .NET types, enabling .NET JIT devirtualization

namespace BenchmarkPhp;

interface CalculatorInterface
{
    public function calculate(int $x, int $y): int;
}

abstract class AbstractCalculator
{
    abstract public function calculate(int $x, int $y): int;
}

class Adder extends AbstractCalculator implements CalculatorInterface
{
    public function calculate(int $x, int $y): int
    {
        return $x + $y;
    }
}

class Multiplier extends AbstractCalculator implements CalculatorInterface
{
    public function calculate(int $x, int $y): int
    {
        return $x * $y;
    }
}

class ObjectMath
{
    public static function dispatchViaInterface(CalculatorInterface $calc, int $x, int $y): int
    {
        return $calc->calculate($x, $y);
    }

    public static function dispatchViaAbstract(AbstractCalculator $calc, int $x, int $y): int
    {
        return $calc->calculate($x, $y);
    }

    public static function concreteCall(Adder $adder, int $x, int $y): int
    {
        return $adder->calculate($x, $y);
    }

    public static function polymorphicSum(int $iterations): int
    {
        $adder = new Adder();
        $multiplier = new Multiplier();
        $sum = 0;
        for ($i = 0; $i < $iterations; $i++) {
            if ($i % 2 === 0) {
                $sum += $adder->calculate($i, $i + 1);
            } else {
                $sum += $multiplier->calculate($i, $i + 1);
            }
        }
        return $sum;
    }
}

class Point
{
    public int $x;
    public int $y;

    public function __construct(int $x, int $y)
    {
        $this->x = $x;
        $this->y = $y;
    }

    public function distanceTo(Point $other): float
    {
        $dx = $this->x - $other->x;
        $dy = $this->y - $other->y;
        return sqrt($dx * $dx + $dy * $dy);
    }

    public function getX(): int
    {
        return $this->x;
    }

    public function getY(): int
    {
        return $this->y;
    }

    public function setX(int $x): void
    {
        $this->x = $x;
    }

    public function setY(int $y): void
    {
        $this->y = $y;
    }
}

class PointBenchmark
{
    public static function createPoints(int $count): array
    {
        $points = [];
        for ($i = 0; $i < $count; $i++) {
            $points[] = new Point($i, $i * 2);
        }
        return $points;
    }

    public static function sumDistances(array $points): float
    {
        $sum = 0.0;
        $origin = new Point(0, 0);
        foreach ($points as $p) {
            $sum += $p->distanceTo($origin);
        }
        return $sum;
    }

    public static function accessors(array $points): int
    {
        $sum = 0;
        foreach ($points as $p) {
            $sum += $p->getX() + $p->getY();
        }
        return $sum;
    }
}
