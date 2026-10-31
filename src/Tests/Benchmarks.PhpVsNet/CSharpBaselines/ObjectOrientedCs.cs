using System;

namespace BenchmarkCs
{
    public interface ICalculatorInterface
    {
        int Calculate(int x, int y);
    }

    public abstract class AbstractCalculator
    {
        public abstract int Calculate(int x, int y);
    }

    public class Adder : AbstractCalculator, ICalculatorInterface
    {
        public override int Calculate(int x, int y) => x + y;
    }

    public class Multiplier : AbstractCalculator, ICalculatorInterface
    {
        public override int Calculate(int x, int y) => x * y;
    }

    public static class ObjectMathCs
    {
        public static int DispatchViaInterface(ICalculatorInterface calc, int x, int y)
        {
            return calc.Calculate(x, y);
        }

        public static int DispatchViaAbstract(AbstractCalculator calc, int x, int y)
        {
            return calc.Calculate(x, y);
        }

        public static int ConcreteCall(Adder adder, int x, int y)
        {
            return adder.Calculate(x, y);
        }

        public static int PolymorphicSum(int iterations)
        {
            var adder = new Adder();
            var multiplier = new Multiplier();
            int sum = 0;
            for (int i = 0; i < iterations; i++)
            {
                if (i % 2 == 0)
                {
                    sum += adder.Calculate(i, i + 1);
                }
                else
                {
                    sum += multiplier.Calculate(i, i + 1);
                }
            }
            return sum;
        }
    }

    public class PointCs
    {
        public int X { get; set; }
        public int Y { get; set; }

        public PointCs(int x, int y)
        {
            X = x;
            Y = y;
        }

        public double DistanceTo(PointCs other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public static class PointBenchmarkCs
    {
        public static PointCs[] CreatePoints(int count)
        {
            var points = new PointCs[count];
            for (int i = 0; i < count; i++)
            {
                points[i] = new PointCs(i, i * 2);
            }
            return points;
        }

        public static double SumDistances(PointCs[] points)
        {
            double sum = 0.0;
            var origin = new PointCs(0, 0);
            foreach (var p in points)
            {
                sum += p.DistanceTo(origin);
            }
            return sum;
        }

        public static int Accessors(PointCs[] points)
        {
            int sum = 0;
            foreach (var p in points)
            {
                sum += p.X + p.Y;
            }
            return sum;
        }
    }
}
